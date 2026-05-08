-- Muhasebe MVP: çok şirket, cari, fatura, hareket, ek dosya
IF OBJECT_ID(N'dbo.AccountingCompanies', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AccountingCompanies](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LegalName] [nvarchar](300) NOT NULL,
        [TaxId] [nvarchar](20) NULL,
        [TaxOffice] [nvarchar](150) NULL,
        [DefaultCurrency] [char](3) NOT NULL CONSTRAINT [DF_AccountingCompanies_DefaultCurrency] DEFAULT ('TRY'),
        [Address] [nvarchar](500) NULL,
        [Phone] [nvarchar](30) NULL,
        [Email] [nvarchar](100) NULL,
        [IsActive] [bit] NOT NULL CONSTRAINT [DF_AccountingCompanies_IsActive] DEFAULT ((1)),
        [CreatedAt] [datetime2](7) NOT NULL CONSTRAINT [DF_AccountingCompanies_CreatedAt] DEFAULT (sysutcdatetime()),
        [UpdatedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_AccountingCompanies] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF OBJECT_ID(N'dbo.Counterparties', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Counterparties](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CompanyId] [int] NOT NULL,
        [Name] [nvarchar](300) NOT NULL,
        [CounterpartyType] [tinyint] NOT NULL CONSTRAINT [DF_Counterparties_Type] DEFAULT ((3)),
        [TaxId] [nvarchar](20) NULL,
        [Email] [nvarchar](100) NULL,
        [Phone] [nvarchar](30) NULL,
        [Notes] [nvarchar](max) NULL,
        [IsActive] [bit] NOT NULL CONSTRAINT [DF_Counterparties_IsActive] DEFAULT ((1)),
        [CreatedAt] [datetime2](7) NOT NULL CONSTRAINT [DF_Counterparties_CreatedAt] DEFAULT (sysutcdatetime()),
        [UpdatedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_Counterparties] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Counterparties_AccountingCompanies] FOREIGN KEY([CompanyId]) REFERENCES [dbo].[AccountingCompanies] ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Counterparties_CompanyId_Name] ON [dbo].[Counterparties]([CompanyId] ASC, [Name] ASC);
END
GO

IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Invoices](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CompanyId] [int] NOT NULL,
        [CounterpartyId] [int] NOT NULL,
        [Direction] [tinyint] NOT NULL,
        [InvoiceNo] [nvarchar](50) NOT NULL,
        [IssueDate] [date] NOT NULL,
        [DueDate] [date] NULL,
        [Currency] [char](3) NOT NULL CONSTRAINT [DF_Invoices_Currency] DEFAULT ('TRY'),
        [AmountNet] [decimal](18, 2) NOT NULL CONSTRAINT [DF_Invoices_AmountNet] DEFAULT ((0)),
        [AmountVat] [decimal](18, 2) NOT NULL CONSTRAINT [DF_Invoices_AmountVat] DEFAULT ((0)),
        [AmountGross] [decimal](18, 2) NOT NULL CONSTRAINT [DF_Invoices_AmountGross] DEFAULT ((0)),
        [Notes] [nvarchar](max) NULL,
        [CreatedByAdminId] [int] NULL,
        [CreatedAt] [datetime2](7) NOT NULL CONSTRAINT [DF_Invoices_CreatedAt] DEFAULT (sysutcdatetime()),
        [UpdatedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Invoices_AccountingCompanies] FOREIGN KEY([CompanyId]) REFERENCES [dbo].[AccountingCompanies] ([Id]),
        CONSTRAINT [FK_Invoices_Counterparties] FOREIGN KEY([CounterpartyId]) REFERENCES [dbo].[Counterparties] ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Invoices_CompanyId_IssueDate] ON [dbo].[Invoices]([CompanyId] ASC, [IssueDate] DESC);
    CREATE NONCLUSTERED INDEX [IX_Invoices_CompanyId_CounterpartyId] ON [dbo].[Invoices]([CompanyId] ASC, [CounterpartyId] ASC);
    CREATE UNIQUE NONCLUSTERED INDEX [UX_Invoices_CompanyId_InvoiceNo] ON [dbo].[Invoices]([CompanyId] ASC, [InvoiceNo] ASC);
END
GO

IF OBJECT_ID(N'dbo.InvoiceAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InvoiceAttachments](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [InvoiceId] [int] NOT NULL,
        [OriginalFileName] [nvarchar](260) NOT NULL,
        [ContentType] [nvarchar](100) NOT NULL,
        [StorageKey] [nvarchar](500) NOT NULL,
        [FileSizeBytes] [bigint] NOT NULL CONSTRAINT [DF_InvoiceAttachments_FileSize] DEFAULT ((0)),
        [Sha256Hex] [char](64) NULL,
        [UploadedByAdminId] [int] NULL,
        [UploadedAt] [datetime2](7) NOT NULL CONSTRAINT [DF_InvoiceAttachments_UploadedAt] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_InvoiceAttachments] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_InvoiceAttachments_Invoices] FOREIGN KEY([InvoiceId]) REFERENCES [dbo].[Invoices] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_InvoiceAttachments_InvoiceId] ON [dbo].[InvoiceAttachments]([InvoiceId] ASC);
END
GO

IF OBJECT_ID(N'dbo.AccountMovements', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AccountMovements](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CompanyId] [int] NOT NULL,
        [CounterpartyId] [int] NOT NULL,
        [MovementDate] [date] NOT NULL,
        [IsCredit] [bit] NOT NULL,
        [Amount] [decimal](18, 2) NOT NULL,
        [Currency] [char](3) NOT NULL CONSTRAINT [DF_AccountMovements_Currency] DEFAULT ('TRY'),
        [ReferenceType] [varchar](20) NOT NULL,
        [ReferenceId] [int] NULL,
        [Description] [nvarchar](500) NULL,
        [CreatedByAdminId] [int] NULL,
        [CreatedAt] [datetime2](7) NOT NULL CONSTRAINT [DF_AccountMovements_CreatedAt] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_AccountMovements] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AccountMovements_AccountingCompanies] FOREIGN KEY([CompanyId]) REFERENCES [dbo].[AccountingCompanies] ([Id]),
        CONSTRAINT [FK_AccountMovements_Counterparties] FOREIGN KEY([CounterpartyId]) REFERENCES [dbo].[Counterparties] ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_AccountMovements_Company_Counterparty_Date] ON [dbo].[AccountMovements]([CompanyId] ASC, [CounterpartyId] ASC, [MovementDate] DESC);
END
GO
