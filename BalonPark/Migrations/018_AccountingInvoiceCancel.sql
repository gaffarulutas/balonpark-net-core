-- Fatura iptal bayrağı
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Invoices') AND name = N'IsCancelled')
BEGIN
    ALTER TABLE [dbo].[Invoices] ADD [IsCancelled] [bit] NOT NULL CONSTRAINT [DF_Invoices_IsCancelled] DEFAULT ((0));
END
GO
