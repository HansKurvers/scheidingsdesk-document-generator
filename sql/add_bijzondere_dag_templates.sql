-- SQL Script to add "Bijzondere dag" template type
-- This script adds example templates for the new "Bijzondere dag" type

-- Check if the template type already exists
IF NOT EXISTS (SELECT 1 FROM dbo.regelingen_templates WHERE type = 'Bijzondere dag')
BEGIN
    -- Insert templates for "Bijzondere dag"
    INSERT INTO dbo.regelingen_templates (template_naam, template_tekst, meervoud_kinderen, type)
    VALUES 
    -- Single child templates
    ('partij1_bijzonder', '{KIND} is tijdens {BIJZONDERE_DAG} bij {PARTIJ1}', 0, 'Bijzondere dag'),
    ('partij2_bijzonder', '{KIND} is tijdens {BIJZONDERE_DAG} bij {PARTIJ2}', 0, 'Bijzondere dag'),
    ('partij1_even_bijzonder', '{KIND} is tijdens {BIJZONDERE_DAG} in even jaren bij {PARTIJ1}', 0, 'Bijzondere dag'),
    ('partij2_even_bijzonder', '{KIND} is tijdens {BIJZONDERE_DAG} in even jaren bij {PARTIJ2}', 0, 'Bijzondere dag'),
    ('partij1_oneven_bijzonder', '{KIND} is tijdens {BIJZONDERE_DAG} in oneven jaren bij {PARTIJ1}', 0, 'Bijzondere dag'),
    ('partij2_oneven_bijzonder', '{KIND} is tijdens {BIJZONDERE_DAG} in oneven jaren bij {PARTIJ2}', 0, 'Bijzondere dag'),
    
    -- Multiple children templates
    ('partij1_bijzonder_kinderen', 'De kinderen zijn tijdens {BIJZONDERE_DAG} bij {PARTIJ1}', 1, 'Bijzondere dag'),
    ('partij2_bijzonder_kinderen', 'De kinderen zijn tijdens {BIJZONDERE_DAG} bij {PARTIJ2}', 1, 'Bijzondere dag'),
    ('partij1_even_bijzonder_kinderen', 'De kinderen zijn tijdens {BIJZONDERE_DAG} in even jaren bij {PARTIJ1}', 1, 'Bijzondere dag'),
    ('partij2_even_bijzonder_kinderen', 'De kinderen zijn tijdens {BIJZONDERE_DAG} in even jaren bij {PARTIJ2}', 1, 'Bijzondere dag'),
    ('partij1_oneven_bijzonder_kinderen', 'De kinderen zijn tijdens {BIJZONDERE_DAG} in oneven jaren bij {PARTIJ1}', 1, 'Bijzondere dag'),
    ('partij2_oneven_bijzonder_kinderen', 'De kinderen zijn tijdens {BIJZONDERE_DAG} in oneven jaren bij {PARTIJ2}', 1, 'Bijzondere dag'),
    
    -- Shared/alternating templates
    ('wisselend_bijzonder', '{KIND} is tijdens {BIJZONDERE_DAG} afwisselend bij {PARTIJ1} en {PARTIJ2}', 0, 'Bijzondere dag'),
    ('wisselend_bijzonder_kinderen', 'De kinderen zijn tijdens {BIJZONDERE_DAG} afwisselend bij {PARTIJ1} en {PARTIJ2}', 1, 'Bijzondere dag'),
    ('samen_bijzonder', '{KIND} viert {BIJZONDERE_DAG} samen met beide ouders', 0, 'Bijzondere dag'),
    ('samen_bijzonder_kinderen', 'De kinderen vieren {BIJZONDERE_DAG} samen met beide ouders', 1, 'Bijzondere dag');

    PRINT 'Successfully added "Bijzondere dag" templates';
END
ELSE
BEGIN
    PRINT '"Bijzondere dag" templates already exist in the database';
END

-- Verify the templates were added
SELECT type, COUNT(*) as template_count 
FROM dbo.regelingen_templates 
GROUP BY type 
ORDER BY type;