-- Debug script for ouderschapsplan placeholder issues
-- This script will help identify why [[RelatieAanvangZin]] and [[OuderschapsplanDoelZin]] aren't being replaced

-- 1. Check if ouderschapsplan_info record exists for dossier 69
SELECT '=== 1. CHECKING IF RECORD EXISTS ===' as debug_step;
SELECT COUNT(*) as record_count 
FROM ouderschapsplan_info 
WHERE dossier_id = 69;

-- 2. Show all data for dossier 69
SELECT '=== 2. FULL RECORD DATA ===' as debug_step;
SELECT * FROM ouderschapsplan_info 
WHERE dossier_id = 69\G

-- 3. Check table structure to see which columns exist
SELECT '=== 3. TABLE COLUMNS ===' as debug_step;
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ouderschapsplan_info' 
AND TABLE_SCHEMA = DATABASE()
ORDER BY ORDINAL_POSITION;

-- 4. Specifically check for the generated text columns
SELECT '=== 4. GENERATED TEXT COLUMNS ===' as debug_step;
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ouderschapsplan_info' 
AND TABLE_SCHEMA = DATABASE()
AND COLUMN_NAME IN ('gezag_zin', 'relatie_aanvang_zin', 'ouderschapsplan_doel_zin');

-- 5. Check soort_relatie value specifically
SELECT '=== 5. SOORT_RELATIE VALUE ===' as debug_step;
SELECT 
    dossier_id,
    soort_relatie,
    CASE 
        WHEN soort_relatie IS NULL THEN 'NULL'
        WHEN soort_relatie = '' THEN 'EMPTY STRING'
        ELSE CONCAT('Value: "', soort_relatie, '"')
    END as soort_relatie_status
FROM ouderschapsplan_info 
WHERE dossier_id = 69;

-- 6. Check date fields
SELECT '=== 6. DATE FIELDS ===' as debug_step;
SELECT 
    dossier_id,
    datum_aanvang_relatie,
    DATE_FORMAT(datum_aanvang_relatie, '%d %M %Y') as formatted_date
FROM ouderschapsplan_info 
WHERE dossier_id = 69;

-- 7. Check plaats_relatie
SELECT '=== 7. PLAATS_RELATIE ===' as debug_step;
SELECT 
    dossier_id,
    plaats_relatie,
    CASE 
        WHEN plaats_relatie IS NULL THEN 'NULL'
        WHEN plaats_relatie = '' THEN 'EMPTY STRING'
        ELSE CONCAT('Value: "', plaats_relatie, '"')
    END as plaats_relatie_status
FROM ouderschapsplan_info 
WHERE dossier_id = 69;

-- 8. If generated columns exist, check their values
SELECT '=== 8. GENERATED TEXT VALUES ===' as debug_step;
SELECT 
    dossier_id,
    CASE 
        WHEN gezag_zin IS NULL THEN 'NULL'
        WHEN gezag_zin = '' THEN 'EMPTY STRING'
        ELSE 'HAS VALUE'
    END as gezag_zin_status,
    CASE 
        WHEN relatie_aanvang_zin IS NULL THEN 'NULL'
        WHEN relatie_aanvang_zin = '' THEN 'EMPTY STRING'
        ELSE 'HAS VALUE'
    END as relatie_aanvang_zin_status,
    CASE 
        WHEN ouderschapsplan_doel_zin IS NULL THEN 'NULL'
        WHEN ouderschapsplan_doel_zin = '' THEN 'EMPTY STRING'
        ELSE 'HAS VALUE'
    END as ouderschapsplan_doel_zin_status
FROM ouderschapsplan_info 
WHERE dossier_id = 69;

-- 9. Show actual generated text if available
SELECT '=== 9. ACTUAL GENERATED TEXT ===' as debug_step;
SELECT 
    dossier_id,
    LEFT(relatie_aanvang_zin, 100) as relatie_aanvang_preview,
    LEFT(ouderschapsplan_doel_zin, 100) as ouderschapsplan_doel_preview
FROM ouderschapsplan_info 
WHERE dossier_id = 69;

-- 10. Check if placeholders table exists and has these entries
SELECT '=== 10. PLACEHOLDER TABLE CHECK ===' as debug_step;
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'placeholders' 
AND TABLE_SCHEMA = DATABASE();

-- 11. If placeholders table exists, check entries
SELECT '=== 11. PLACEHOLDER ENTRIES ===' as debug_step;
SELECT * FROM placeholders 
WHERE placeholder_name IN ('RelatieAanvangZin', 'OuderschapsplanDoelZin');

-- 12. Check document templates table
SELECT '=== 12. DOCUMENT TEMPLATE CHECK ===' as debug_step;
SELECT 
    id,
    template_name,
    CASE 
        WHEN template_content LIKE '%[[RelatieAanvangZin]]%' THEN 'Contains RelatieAanvangZin'
        ELSE 'Missing RelatieAanvangZin'
    END as relatie_check,
    CASE 
        WHEN template_content LIKE '%[[OuderschapsplanDoelZin]]%' THEN 'Contains OuderschapsplanDoelZin'
        ELSE 'Missing OuderschapsplanDoelZin'
    END as ouderschapsplan_check
FROM document_templates 
WHERE template_name LIKE '%ouderschapsplan%';

-- 13. Summary - show what replacement values would be
SELECT '=== 13. REPLACEMENT VALUES SUMMARY ===' as debug_step;
SELECT 
    o.dossier_id,
    o.soort_relatie,
    o.datum_aanvang_relatie,
    o.plaats_relatie,
    COALESCE(o.relatie_aanvang_zin, 'WOULD BE GENERATED') as relatie_zin,
    COALESCE(o.ouderschapsplan_doel_zin, 'WOULD BE GENERATED') as doel_zin,
    (SELECT COUNT(*) FROM dossiers_kinderen WHERE dossier_id = o.dossier_id) as aantal_kinderen
FROM ouderschapsplan_info o
WHERE o.dossier_id = 69;

-- 14. Check how many records have API-generated content
SELECT '=== 14. API GENERATION STATS ===' as debug_step;
SELECT 
    COUNT(*) as total_records,
    SUM(CASE WHEN relatie_aanvang_zin IS NOT NULL AND relatie_aanvang_zin != '' THEN 1 ELSE 0 END) as has_relatie_zin,
    SUM(CASE WHEN ouderschapsplan_doel_zin IS NOT NULL AND ouderschapsplan_doel_zin != '' THEN 1 ELSE 0 END) as has_doel_zin
FROM ouderschapsplan_info;