#!/usr/bin/env python3
"""
Анализатор TRX файлов для ClubDoorman тестов
Конвертирует TRX в JSON с группировкой ошибок по типам
"""

import xml.etree.ElementTree as ET
import json
import re
from collections import defaultdict
from typing import Dict, List, Any

def parse_trx(trx_file: str) -> Dict[str, Any]:
    """Парсит TRX файл и возвращает структурированные данные"""
    
    # Регистрируем namespace
    ET.register_namespace('', "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
    
    tree = ET.parse(trx_file)
    root = tree.getroot()
    
    # Извлекаем статистику
    total_tests = 0
    passed_tests = 0
    failed_tests = 0
    skipped_tests = 0
    
    # Группируем ошибки по типам
    error_groups = defaultdict(list)
    
    # Обрабатываем результаты тестов
    for result in root.findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult'):
        total_tests += 1
        test_name = result.get('testName', 'Unknown')
        outcome = result.get('outcome', 'Unknown')
        duration = result.get('duration', '0')
        
        test_info = {
            'name': test_name,
            'outcome': outcome,
            'duration': duration,
            'file': extract_file_from_test_name(test_name)
        }
        
        if outcome == 'Passed':
            passed_tests += 1
        elif outcome == 'Failed':
            failed_tests += 1
            # Извлекаем информацию об ошибке
            error_info = extract_error_info(result)
            if error_info:
                error_type = classify_error(error_info['message'])
                error_groups[error_type].append({
                    'test_name': test_name,
                    'message': error_info['message'],
                    'stack_trace': error_info['stack_trace'],
                    'file': test_info['file']
                })
        elif outcome == 'Skipped':
            skipped_tests += 1
    
    return {
        'summary': {
            'total': total_tests,
            'passed': passed_tests,
            'failed': failed_tests,
            'skipped': skipped_tests,
            'success_rate': round((passed_tests / total_tests * 100), 2) if total_tests > 0 else 0
        },
        'error_groups': dict(error_groups),
        'error_analysis': analyze_error_patterns(error_groups)
    }

def extract_file_from_test_name(test_name: str) -> str:
    """Извлекает имя файла из названия теста"""
    # Ищем паттерны типа ClassName_MethodName
    parts = test_name.split('_')
    if len(parts) >= 2:
        return f"{parts[0]}.cs"
    return "Unknown.cs"

def extract_error_info(result) -> Dict[str, str]:
    """Извлекает информацию об ошибке из результата теста"""
    error_info = result.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}ErrorInfo')
    if error_info is not None:
        message_elem = error_info.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Message')
        stack_elem = error_info.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}StackTrace')
        
        message = message_elem.text if message_elem is not None else "No message"
        stack_trace = stack_elem.text if stack_elem is not None else "No stack trace"
        
        return {
            'message': message,
            'stack_trace': stack_trace
        }
    return None

def classify_error(message: str) -> str:
    """Классифицирует ошибку по типу"""
    message_lower = message.lower()
    
    if 'nullreferenceexception' in message_lower:
        return 'NullReferenceException'
    elif 'argumentnullexception' in message_lower:
        return 'ArgumentNullException'
    elif 'argumentexception' in message_lower:
        return 'ArgumentException'
    elif 'invalidoperationexception' in message_lower:
        return 'InvalidOperationException'
    elif 'moderationexception' in message_lower:
        return 'ModerationException'
    elif 'assert' in message_lower and 'expected' in message_lower:
        return 'AssertionFailure'
    elif 'mock' in message_lower and 'exception' in message_lower:
        return 'MockException'
    elif 'autofixture' in message_lower:
        return 'AutoFixtureException'
    elif 'unauthorized' in message_lower:
        return 'APIError'
    else:
        return 'OtherException'

def analyze_error_patterns(error_groups: Dict) -> Dict[str, Any]:
    """Анализирует паттерны ошибок"""
    analysis = {
        'most_common_errors': [],
        'files_with_most_errors': defaultdict(int),
        'error_patterns': []
    }
    
    # Подсчитываем количество ошибок по типам
    error_counts = {error_type: len(tests) for error_type, tests in error_groups.items()}
    analysis['most_common_errors'] = sorted(error_counts.items(), key=lambda x: x[1], reverse=True)
    
    # Подсчитываем ошибки по файлам
    for error_type, tests in error_groups.items():
        for test in tests:
            analysis['files_with_most_errors'][test['file']] += 1
    
    # Сортируем файлы по количеству ошибок
    analysis['files_with_most_errors'] = dict(
        sorted(analysis['files_with_most_errors'].items(), key=lambda x: x[1], reverse=True)
    )
    
    return analysis

def generate_work_plan(analysis: Dict[str, Any]) -> Dict[str, Any]:
    """Генерирует план работ на основе анализа"""
    
    work_plan = {
        'priority_1': [],  # Критические ошибки
        'priority_2': [],  # Важные ошибки
        'priority_3': [],  # Менее важные ошибки
        'recommendations': []
    }
    
    # Приоритизация по типам ошибок
    for error_type, tests in analysis['error_groups'].items():
        if error_type in ['NullReferenceException', 'InvalidOperationException']:
            work_plan['priority_1'].extend(tests)
        elif error_type in ['ArgumentNullException', 'ArgumentException', 'AssertionFailure']:
            work_plan['priority_2'].extend(tests)
        else:
            work_plan['priority_3'].extend(tests)
    
    # Рекомендации
    if analysis['error_groups'].get('NullReferenceException'):
        work_plan['recommendations'].append({
            'type': 'critical',
            'message': 'Много NullReferenceException - проверить инициализацию моков в TestKit',
            'count': len(analysis['error_groups']['NullReferenceException'])
        })
    
    if analysis['error_groups'].get('InvalidOperationException'):
        work_plan['recommendations'].append({
            'type': 'critical', 
            'message': 'InvalidOperationException - проблемы с DI контейнером, проверить регистрацию IModerationFacade',
            'count': len(analysis['error_groups']['InvalidOperationException'])
        })
    
    if analysis['error_groups'].get('AssertionFailure'):
        work_plan['recommendations'].append({
            'type': 'important',
            'message': 'AssertionFailure - логика тестов не соответствует новой архитектуре',
            'count': len(analysis['error_groups']['AssertionFailure'])
        })
    
    return work_plan

def main():
    """Основная функция"""
    trx_file = './ClubDoorman.Test/TestResults/test-results.trx'
    
    print("🔍 Анализируем TRX файл...")
    analysis = parse_trx(trx_file)
    
    print("📊 Генерируем план работ...")
    work_plan = generate_work_plan(analysis)
    
    # Объединяем все данные
    result = {
        'analysis': analysis,
        'work_plan': work_plan,
        'generated_at': '2025-08-16T03:46:12+03:00'
    }
    
    # Сохраняем в JSON
    with open('test-analysis.json', 'w', encoding='utf-8') as f:
        json.dump(result, f, ensure_ascii=False, indent=2)
    
    # Выводим краткую сводку
    print("\n📈 КРАТКАЯ СВОДКА:")
    print(f"Всего тестов: {analysis['summary']['total']}")
    print(f"Пройдено: {analysis['summary']['passed']}")
    print(f"Провалено: {analysis['summary']['failed']}")
    print(f"Пропущено: {analysis['summary']['skipped']}")
    print(f"Успешность: {analysis['summary']['success_rate']}%")
    
    print("\n🚨 ТОП ОШИБОК:")
    for error_type, count in analysis['error_analysis']['most_common_errors'][:5]:
        print(f"  {error_type}: {count}")
    
    print("\n📁 ФАЙЛЫ С НАИБОЛЬШИМ КОЛИЧЕСТВОМ ОШИБОК:")
    for file, count in list(analysis['error_analysis']['files_with_most_errors'].items())[:5]:
        print(f"  {file}: {count}")
    
    print("\n✅ Анализ сохранен в test-analysis.json")

if __name__ == "__main__":
    main()
