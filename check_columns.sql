-- Check exact columns in ouderschapsplan_info table
SELECT COLUMN_NAME, ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ouderschapsplan_info' 
AND TABLE_SCHEMA = 'dbo'
ORDER BY ORDINAL_POSITION;

-- Also check the exact record for dossier 69
SELECT * FROM dbo.ouderschapsplan_info WHERE dossier_id = 69;