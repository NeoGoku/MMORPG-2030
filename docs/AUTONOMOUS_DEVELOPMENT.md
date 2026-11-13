# Autonomous development protocol

Goal: reduce Android-owner intervention while keeping each Codex Cloud task finite, auditable and productive.

## Start of EVERY Codex task
1. Read AGENTS.md, docs/GDD.md, docs/IMPLEMENTATION_STATUS.md, docs/DEVELOPMENT_HANDOFF.md (if present) and docs/DEVELOPMENT_QUEUE.md.
2. Inspect current branch and open PRs. Never assume unmerged PR changes are on main. Use the latest verified integration base and avoid overwriting other branches.
3. Select the highest-priority feasible unfinished vertical slice. Work on multiple adjacent tasks when time permits.
4. Implement actual server, persistence, client and UI behavior; do not mark mock/stub work complete.
5. Targeted checks only, plus one relevant CI build per coherent batch if possible.
6. Commit and open/update PR. Update handoff, queue and implementation status in the SAME branch.
7. Continue to next feasible item within this task while time remains. PR creation is not itself a stopping criterion.

## End of task
- State branch, PR URL, commit SHA, CI status and what is truly working.
- Distinguish tested / untested / partial / blocked.
- Write exact next steps in docs/DEVELOPMENT_HANDOFF.md.
- Do not merge PRs, provision paid services, change account permissions, or expose secrets without authorization.
- No artificial delays, endless tests or claims of perpetual execution.

## Important limitation
This protocol enables resumable development, NOT automatic scheduling of new Codex Cloud tasks. A separate supported trigger/integration is required. Do not claim an automatic agent loop is active.

## Owner acceptance cadence
Ask for Android hands-on testing only at a meaningful release milestone, security-sensitive change or true blocker, not every microfeature.
