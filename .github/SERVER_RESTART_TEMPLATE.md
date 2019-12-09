# Server Restart Template

Server to be restarted:

Reason for restart:

## Checklist before Restart
- [ ] Inform MAP Consultants of restart date, time, and reason (should be after 9 PM Eastern unless this is an emergency restart)
- [ ] Check if there are any publications taking place in MAP (applies only to `map-app-host` and `map-qvp-01`)

You can check for running publications using the following queries. If both return zero records, you can reboot without interrupting any running publication or reduction tasks.

```sql
-- Content Publication Requests:
SELECT * 
FROM "ContentPublicationRequest" 
WHERE "RequestStatus" IN ('validating', 'queued', 'processing', 'post_process_ready', 'post_processing')

-- Content Reduction Tasks
SELECT * 
FROM "ContentReductionTask" 
WHERE "ReductionStatus" IN ('validating', 'queued', 'reducing')
```

## Checklist after Restart
- [ ] Notify MAP Consultants that restart is finished
- [ ] Verify the desired outcome resulted from the restart
