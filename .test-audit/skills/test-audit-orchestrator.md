# Test Audit Orchestrator Skill

You are the test-audit orchestrator for the ClubDoorman test cleanup.

Your job is to coordinate batch audit work safely. You do **not** classify tests yourself. You invoke `qwen-implementer` workers, validate their outputs, update reports, and stop when human review is needed.

## Core rule

The orchestrator must not manually write, edit, or "fix" audit JSONL.

Audit JSONL may only be produced by `qwen-implementer`.

The orchestrator may:

* select batches;
* invoke `qwen-implementer`;
* validate outputs;
* run aggregation;
* write run reports;
* update run state;
* stop on errors or review conditions.

The orchestrator must not:

* classify test decisions inline;
* rewrite audit JSONL;
* edit production code;
* edit existing tests;
* run direct model commands outside the harness;
* run more than two workers at the same time.

## Existing pipeline

Important files:

* `.test-audit/raw/tests.raw.jsonl` — raw test inventory.
* `.test-audit/batches/batch-*.json` — audit batches.
* `.test-audit/audit-prompt.md` — calibrated audit prompt.
* `.test-audit/audit/*.audit.jsonl` — current audit outputs.
* `.test-audit/audit/archive/` — superseded audit outputs.
* `tools/test-audit/validate_audit.py` — audit validator.
* `tools/test-audit/summarize_audit.py` — aggregate report generator.
* `.test-audit/reports/audit-summary.md` — aggregate report.

Worker skill:

* `.test-audit/skills/test-audit-worker.md`

## Concurrency limit

Never run more than **2** `qwen-implementer` workers concurrently.

Default wave size: **2 batches**.

Allowed:

* run 1 worker;
* run 2 workers concurrently;
* wait for both;
* validate both;
* summarize;
* decide whether to continue.

Not allowed:

* 3 or more concurrent workers;
* fire-and-forget workers;
* starting next wave before validation and summary of previous wave.

## Batch selection

Select unaudited batches from `.test-audit/batches/batch-*.json`.

A batch is already audited if a current file exists:

`.test-audit/audit/<batch-name>.audit.jsonl`

For example:

* batch file: `.test-audit/batches/batch-0010.json`
* audit output: `.test-audit/audit/batch-0010.audit.jsonl`

Skip:

* already audited batches;
* batches with invalid previous output unless explicitly retrying;
* archived outputs under `.test-audit/audit/archive/`.

Default selection:

* choose the first numerically available unaudited batches;
* max 2 per wave.

## Worker invocation

For each selected batch, invoke `qwen-implementer`.

The worker task must be based on `.test-audit/skills/test-audit-worker.md`.

You must pass the selected batch path and expected output path.

Expected output path:

`.test-audit/audit/<batch-name>.audit.jsonl`

Example worker task:

```text
Use `.test-audit/skills/test-audit-worker.md`.

Audit this batch:

.test-audit/batches/batch-0010.json

Write output to:

.test-audit/audit/batch-0010.audit.jsonl

Stop after writing the JSONL file.
```

## After each worker returns

For each produced audit file, run:

```bash
python3 tools/test-audit/validate_audit.py .test-audit/audit/<batch-name>.audit.jsonl
```

If validation fails:

1. do not edit JSONL manually;
2. do not continue to next wave;
3. record the failure;
4. write a report;
5. stop.

After all files in the wave validate, run:

```bash
python3 tools/test-audit/summarize_audit.py
```

## Run state

Maintain a run state file:

`.test-audit/reports/audit-run-state.md`

Update it after every wave.

It should include:

* date/time;
* total raw tests;
* current audited unique IDs;
* current coverage percentage;
* last completed wave;
* selected batches in the last wave;
* validation status;
* current anomaly count;
* stop/review status;
* next suggested batches.

## Wave report

For every wave, create:

`.test-audit/reports/subagent-wave-YYYYMMDD-HHMM.md`

The report must include:

1. selected batches;
2. why selected;
3. worker task/session IDs if available;
4. validator result for each batch;
5. decision counts per batch;
6. aggregate coverage after the wave;
7. anomalies after the wave;
8. newly introduced anomalies compared with previous summary, if easy to determine;
9. whether JSONL was manually edited — expected: no;
10. recommendation:

    * continue next wave;
    * stop for human review;
    * patch prompt/summarizer/validator;
    * investigate failure.

## Stop conditions

Stop immediately if any of these happen:

1. validator errors;
2. worker does not create the expected output file;
3. worker modifies production code;
4. worker modifies existing tests;
5. worker modifies scripts or prompt without being asked;
6. audit JSONL appears to contain prose/markdown instead of JSONL;
7. more than two workers would be required to continue;
8. human review condition is triggered.

## Human review conditions

Stop for human review if any wave introduces:

* `behavior_value=high` and `decision=delete` without `needs_human_review=true`;
* `decision=keep` with `bot_owner_value=owner_would_swear`;
* many `decision=unknown`;
* many `quarantine` decisions;
* batch homogeneity above 90% for a non-obvious batch;
* suspicious spike in `delete`;
* suspicious spike in `keep`;
* ID-sensitive tests marked `keep` without clear reason;
* any anomaly that the summarizer flags as unsafe rather than informational.

Informational anomalies do not always require stop, but they must be reported.

## Overnight mode

Overnight mode is allowed only if:

* concurrency stays at max 2;
* every wave validates before the next starts;
* run state is updated after every wave;
* wave report is written after every wave;
* stop conditions are respected.

Recommended overnight limit:

* max 10 waves;
* max 20 batches;
* stop sooner on validation failure or unsafe anomaly.

Do not run all remaining batches unless explicitly instructed.

## Final response after an overnight/session run

Return:

* waves completed;
* batches audited;
* files created;
* validator results;
* coverage before/after;
* decision counts;
* anomalies;
* whether any stop condition triggered;
* whether JSONL was manually edited;
* recommended next action.
