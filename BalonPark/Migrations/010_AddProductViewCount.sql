-- 010: Add ViewCount column to Products table (default 0, non-nullable)

IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Products')
          AND name = 'ViewCount'
    )
    BEGIN
        ALTER TABLE [dbo].[Products]
        ADD [ViewCount] [int] NOT NULL CONSTRAINT [DF_Products_ViewCount] DEFAULT ((0)) WITH VALUES;
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Products]
        ALTER COLUMN [ViewCount] [int] NOT NULL;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.Products')
              AND c.name = 'ViewCount'
        )
        BEGIN
            ALTER TABLE [dbo].[Products]
            ADD CONSTRAINT [DF_Products_ViewCount] DEFAULT ((0)) FOR [ViewCount];
        END
    END
END
GO
