#!/usr/bin/env python3
"""
Анализатор TRX файлов для ClubDoorman тестов
Конвертирует TRX в удобный JSON формат для LLM анализа
"""

import xml.etree.ElementTree as ET
import json
import sys
import re
from collections import defaultdict
from typing import Dict, List, Any

def parse_trx(trx_file: str) -> Dict[str, Any]:
    """Парсит TRX файл и возвращает структурированные данные"""
    
    tree = ET.parse(trx_file)
    root = tree.getroot()
    
    # Определяем namespace
    ns = {'trx': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    
    results = {
        'summary': {
            'total': 0,
            'passed': 0,
            'failed': 0,
            'skipped': 0
        },
        'errors': defaultdict(list),
        'error_types': defaultdict(int),
        'test_files': defaultdict(list),
        'error_patterns': defaultdict(int)
    }
    
    # Парсим результаты тестов
    for unit_test in root.findall('.//trx:UnitTestResult', ns):
        test_name = unit_test.get('testName', 'Unknown')
        outcome = unit_test.get('outcome', 'Unknown')
        duration = unit_test.get('duration', '0')
        
        # Обновляем статистику
        results['summary']['total'] += 1
        
        if outcome == 'Passed':
            results['summary']['passed'] += 1
        elif outcome == 'Failed':
            results['summary']['failed'] += 1
        elif outcome == 'NotExecuted':
            results['summary']['skipped'] += 1
        
        # Обрабатываем ошибки
        if outcome == 'Failed':
            error_info = {
                'test_name': test_name,
                'duration': duration,
                'error_type': 'Unknown',
                'error_message': '',
                'stack_trace': '',
                'file_location': ''
            }
            
            # Извлекаем информацию об ошибке
            output = unit_test.find('.//trx:Output', ns)
            if output is not None:
                error_info_elem = output.find('.//trx:ErrorInfo', ns)
                if error_info_elem is not None:
                    message_elem = error_info_elem.find('.//trx:Message', ns)
                    if message_elem is not None:
                        error_info['error_message'] = message_elem.text or ''
                    
                    stack_elem = error_info_elem.find('.//trx:StackTrace', ns)
                    if stack_elem is not None:
                        error_info['stack_trace'] = stack_elem.text or ''
            
            # Определяем тип ошибки
            error_type = classify_error(error_info['error_message'], error_info['stack_trace'])
            error_info['error_type'] = error_type
            
            # Извлекаем файл из имени теста
            file_name = extract_file_name(test_name)
            error_info['file_location'] = file_name
            
            # Добавляем в результаты
            results['errors'][error_type].append(error_info)
            results['error_types'][error_type] += 1
            
            # Добавляем в файлы
            results['test_files'][file_name].append(error_info)
            
            # Анализируем паттерны ошибок
            pattern = extract_error_pattern(error_info['error_message'])
            results['error_patterns'][pattern] += 1
    
    return results

def classify_error(message: str, stack_trace: str) -> str:
    """Классифицирует ошибку по типу"""
    message_lower = message.lower()
    stack_lower = stack_trace.lower()
    
    if 'nullreferenceexception' in message_lower or 'nullreferenceexception' in stack_lower:
        return 'NullReferenceException'
    elif 'mockexception' in message_lower or 'mockexception' in stack_lower:
        return 'MockException'
    elif 'argumentexception' in message_lower or 'argumentexception' in stack_lower:
        return 'ArgumentException'
    elif 'argumentnullexception' in message_lower or 'argumentnullexception' in stack_lower:
        return 'ArgumentNullException'
    elif 'invalidoperationexception' in message_lower or 'invalidoperationexception' in stack_lower:
        return 'InvalidOperationException'
    elif 'missingmethodexception' in message_lower or 'missingmethodexception' in stack_lower:
        return 'MissingMethodException'
    elif 'autofixture' in message_lower:
        return 'AutoFixtureException'
    elif 'assert' in message_lower and 'expected' in message_lower:
        return 'AssertionFailure'
    elif 'constructor' in message_lower and 'not found' in message_lower:
        return 'ConstructorNotFound'
    elif 'unable to resolve service' in message_lower:
        return 'DependencyInjectionError'
    else:
        return 'OtherException'

def extract_file_name(test_name: str) -> str:
    """Извлекает имя файла из имени теста"""
    # Ищем паттерны типа ClassName.MethodName
    parts = test_name.split('.')
    if len(parts) >= 2:
        class_name = parts[-2]
        # Убираем суффиксы типа Tests, Test
        if class_name.endswith('Tests'):
            class_name = class_name[:-5]
        elif class_name.endswith('Test'):
            class_name = class_name[:-4]
        return f"{class_name}.cs"
    return "Unknown.cs"

def extract_error_pattern(message: str) -> str:
    """Извлекает паттерн ошибки для группировки"""
    if not message:
        return "Empty message"
    
    # Убираем специфичные значения, оставляем структуру
    pattern = re.sub(r'\d+', 'N', message)
    pattern = re.sub(r'[a-f0-9]{8,}', 'GUID', pattern)
    pattern = re.sub(r'[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}', 'EMAIL', pattern)
    
    return pattern[:100]  # Ограничиваем длину

def generate_work_plan(results: Dict[str, Any]) -> Dict[str, Any]:
    """Генерирует план работ на основе анализа ошибок"""
    
    work_plan = {
        'priority_batches': [],
        'recommendations': [],
        'estimated_effort': {}
    }
    
    # Сортируем ошибки по частоте
    error_counts = dict(results['error_types'])
    sorted_errors = sorted(error_counts.items(), key=lambda x: x[1], reverse=True)
    
    # Батч 1: Критические ошибки (NullReferenceException, ConstructorNotFound)
    critical_errors = [error for error, count in sorted_errors 
                      if error in ['NullReferenceException', 'ConstructorNotFound', 'DependencyInjectionError']]
    if critical_errors:
        work_plan['priority_batches'].append({
            'batch_name': 'Batch 1: Critical Infrastructure Errors',
            'error_types': critical_errors,
            'total_errors': sum(error_counts[error] for error in critical_errors),
            'priority': 'Critical',
            'description': 'Исправление критических ошибок инфраструктуры, которые блокируют работу тестов'
        })
    
    # Батч 2: MockException (проблемы с моками)
    mock_errors = [error for error, count in sorted_errors if error == 'MockException']
    if mock_errors:
        work_plan['priority_batches'].append({
            'batch_name': 'Batch 2: Mock Configuration Issues',
            'error_types': mock_errors,
            'total_errors': sum(error_counts[error] for error in mock_errors),
            'priority': 'High',
            'description': 'Исправление проблем с настройкой моков и ожидаемыми вызовами'
        })
    
    # Батч 3: AssertionFailure (логические ошибки в тестах)
    assertion_errors = [error for error, count in sorted_errors if error == 'AssertionFailure']
    if assertion_errors:
        work_plan['priority_batches'].append({
            'batch_name': 'Batch 3: Test Logic Issues',
            'error_types': assertion_errors,
            'total_errors': sum(error_counts[error] for error in assertion_errors),
            'priority': 'Medium',
            'description': 'Исправление логических ошибок в тестах и несоответствий ожиданий'
        })
    
    # Батч 4: Остальные ошибки
    other_errors = [error for error, count in sorted_errors 
                   if error not in ['NullReferenceException', 'ConstructorNotFound', 'DependencyInjectionError', 
                                   'MockException', 'AssertionFailure']]
    if other_errors:
        work_plan['priority_batches'].append({
            'batch_name': 'Batch 4: Other Issues',
            'error_types': other_errors,
            'total_errors': sum(error_counts[error] for error in other_errors),
            'priority': 'Low',
            'description': 'Исправление остальных типов ошибок'
        })
    
    # Рекомендации
    if results['summary']['failed'] > 0:
        work_plan['recommendations'].append({
            'type': 'Immediate',
            'description': f"Начать с Batch 1 - исправить {work_plan['priority_batches'][0]['total_errors']} критических ошибок"
        })
    
    if error_counts.get('MockException', 0) > 10:
        work_plan['recommendations'].append({
            'type': 'Strategy',
            'description': "Создать централизованную систему настройки моков для предотвращения MockException"
        })
    
    if error_counts.get('AssertionFailure', 0) > 20:
        work_plan['recommendations'].append({
            'type': 'Process',
            'description': "Провести ревизию тестовых данных и ожиданий в TestKit"
        })
    
    # Оценка усилий
    total_failed = results['summary']['failed']
    work_plan['estimated_effort'] = {
        'total_failed_tests': total_failed,
        'estimated_hours': total_failed * 0.1,  # ~6 минут на тест
        'estimated_batches': len(work_plan['priority_batches']),
        'success_rate_target': f"{results['summary']['passed'] / results['summary']['total'] * 100:.1f}% -> 95%"
    }
    
    return work_plan

def main():
    if len(sys.argv) != 2:
        print("Usage: python3 analyze_trx.py <trx_file>")
        sys.exit(1)
    
    trx_file = sys.argv[1]
    
    try:
        print(f"Анализирую TRX файл: {trx_file}")
        results = parse_trx(trx_file)
        
        # Генерируем план работ
        work_plan = generate_work_plan(results)
        
        # Объединяем результаты
        full_report = {
            'analysis_timestamp': '2025-08-16T04:10:53Z',
            'trx_file': trx_file,
            'results': results,
            'work_plan': work_plan
        }
        
        # Сохраняем в JSON
        output_file = 'test-analysis.json'
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(full_report, f, indent=2, ensure_ascii=False)
        
        print(f"✅ Анализ завершен! Результаты сохранены в {output_file}")
        print(f"📊 Статистика: {results['summary']['total']} тестов, {results['summary']['failed']} провалено")
        print(f"🎯 План работ: {len(work_plan['priority_batches'])} батчей для исправления")
        
        # Выводим краткую сводку
        print("\n📋 Краткая сводка ошибок:")
        for error_type, count in sorted(results['error_types'].items(), key=lambda x: x[1], reverse=True):
            print(f"  • {error_type}: {count} ошибок")
        
        print(f"\n🚀 Рекомендуемый следующий шаг: {work_plan['recommendations'][0]['description'] if work_plan['recommendations'] else 'Анализ завершен'}")
        
    except Exception as e:
        print(f"❌ Ошибка при анализе: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
