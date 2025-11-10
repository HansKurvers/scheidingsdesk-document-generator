-- Migration: Add computed placeholder columns to ouderschapsplan_info table
-- Date: 2025-11-10
-- Description: These columns store API-generated placeholder sentences for consistency across documents

-- Check if the columns don't already exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ouderschapsplan_info]') AND name = 'gezag_zin')
BEGIN
    ALTER TABLE [dbo].[ouderschapsplan_info]
    ADD [gezag_zin] NVARCHAR(500) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ouderschapsplan_info]') AND name = 'relatie_aanvang_zin')
BEGIN
    ALTER TABLE [dbo].[ouderschapsplan_info]
    ADD [relatie_aanvang_zin] NVARCHAR(500) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ouderschapsplan_info]') AND name = 'ouderschapsplan_doel_zin')
BEGIN
    ALTER TABLE [dbo].[ouderschapsplan_info]
    ADD [ouderschapsplan_doel_zin] NVARCHAR(500) NULL;
END

-- Add comments to document the purpose of these columns
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'API-generated sentence describing the parental authority arrangement', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE',  @level1name = N'ouderschapsplan_info', 
    @level2type = N'COLUMN', @level2name = N'gezag_zin';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'API-generated sentence describing when and where the relationship started', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE',  @level1name = N'ouderschapsplan_info', 
    @level2type = N'COLUMN', @level2name = N'relatie_aanvang_zin';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'API-generated sentence describing the purpose of the parenting plan', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE',  @level1name = N'ouderschapsplan_info', 
    @level2type = N'COLUMN', @level2name = N'ouderschapsplan_doel_zin';