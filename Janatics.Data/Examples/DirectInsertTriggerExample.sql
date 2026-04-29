-- Example: Setting up DirectInsert trigger for brf_case table
-- This will automatically create case participants when a new case is created

-- 1. Create the trigger configuration
INSERT INTO kat_dag_triggers (
    id,
    table_name,
    trigger,
    dag_id,
    enabled,
    trigger_type,
    insert_config,
    created_at,
    updated_at
) VALUES (
    gen_random_uuid(),
    'brf_case',
    'Insert',
    '00000000-0000-0000-0000-000000000000', -- Not used for DirectInsert
    true,
    'DirectInsert',
    '{
        "brf_caseparticipant": [
            {
                "renprops": "{}",
                "brf_partyroleid": "aa9c1d54-2b0a-4571-a24a-8f4a502d00ec",
                "brf_name": "Default Participant",
                "brf_address": "System Generated",
                "brf_email": "system@example.com",
                "brf_mobilenumber": "+91 00000 00000",
                "uid": "system_generated",
                "brf_caseid": "|RENGUID|"
            },
            {
                "renprops": "{}",
                "userid": "5a951b1c-16cc-473f-b231-97d52fa87b6e",
                "username": "system",
                "email": "system@yopmail.com",
                "brf_partyroleid": "ac41c3bc-193b-416f-8b44-29f50ba05a3b",
                "brf_name": "System Administrator",
                "brf_address": "System Generated",
                "brf_email": "admin@system.com",
                "brf_mobilenumber": "+91 99999 99999",
                "uid": "system_admin",
                "brf_caseid": "|RENGUID|"
            }
        ],
        "brf_case_audit": [
            {
                "action": "CASE_CREATED",
                "description": "Case created automatically by system",
                "created_date": "|Todaydate|",
                "case_id": "|RENGUID|",
                "user_id": "system"
            }
        ]
    }'::jsonb,
    NOW(),
    NOW()
);

-- 2. Test by creating a case record
-- When you insert into brf_case, the trigger will automatically create:
-- - 2 case participants with the case ID
-- - 1 audit record with current date

-- Example transaction request that would trigger this:
/*
{
    "transactionEntityName": "brf_case",
    "extendedProperties": {
        "case_title": "Test Case",
        "case_description": "This is a test case",
        "status": "ACTIVE"
    },
    "useModelBinding": true
}
*/

-- 3. Verify the trigger works by checking the logs
SELECT 
    tl.*,
    tc.table_name,
    tc.trigger_type,
    tc.insert_config
FROM kat_dag_trigger_logs tl
JOIN kat_dag_triggers tc ON tl.trigger_id = tc.id
WHERE tc.table_name = 'brf_case'
ORDER BY tl.created_at DESC
LIMIT 5;

-- 4. Check if child records were created
SELECT 
    'brf_caseparticipant' as table_name,
    COUNT(*) as record_count
FROM brf_caseparticipant 
WHERE brf_caseid = 'YOUR_CASE_ID_HERE'

UNION ALL

SELECT 
    'brf_case_audit' as table_name,
    COUNT(*) as record_count
FROM brf_case_audit 
WHERE case_id = 'YOUR_CASE_ID_HERE';