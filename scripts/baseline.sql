CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Companies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [StreetAddress] nvarchar(100) NULL,
    [City] nvarchar(50) NULL,
    [PostalCode] nvarchar(10) NULL,
    [State] nvarchar(50) NULL,
    [Country] nvarchar(50) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ProcessedStripeEvents] (
    [Id] int NOT NULL IDENTITY,
    [EventId] nvarchar(max) NOT NULL,
    [ProcessedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProcessedStripeEvents] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SizeSystems] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [SizeType] nvarchar(20) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IconClass] nvarchar(50) NULL,
    [AlertClass] nvarchar(50) NULL,
    CONSTRAINT [PK_SizeSystems] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(50) NOT NULL,
    [StreetAddress] nvarchar(100) NULL,
    [City] nvarchar(50) NULL,
    [PostalCode] nvarchar(10) NULL,
    [State] nvarchar(50) NULL,
    [Country] nvarchar(50) NOT NULL,
    [CompanyId] int NULL,
    [IsActive] bit NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUsers_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id])
);
GO


CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(30) NOT NULL,
    [SizeSystemId] int NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_SizeSystems_SizeSystemId] FOREIGN KEY ([SizeSystemId]) REFERENCES [SizeSystems] ([Id])
);
GO


CREATE TABLE [SizeValues] (
    [Id] int NOT NULL IDENTITY,
    [SizeSystemId] int NOT NULL,
    [Value] nvarchar(50) NOT NULL,
    [DisplayText] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_SizeValues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SizeValues_SizeSystems_SizeSystemId] FOREIGN KEY ([SizeSystemId]) REFERENCES [SizeSystems] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [OrderHeaders] (
    [Id] int NOT NULL IDENTITY,
    [ApplicationUserId] nvarchar(450) NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [OrderTotal] decimal(18,2) NOT NULL,
    [OrderStatus] nvarchar(max) NULL,
    [PaymentStatus] nvarchar(max) NULL,
    [PaymentDate] datetime2 NULL,
    [PaymentDueDate] date NULL,
    [ReturnExpirationDate] datetime2 NULL,
    [PaymentIntentId] nvarchar(max) NULL,
    [Name] nvarchar(50) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [StreetAddress] nvarchar(100) NOT NULL,
    [City] nvarchar(50) NOT NULL,
    [State] nvarchar(50) NULL,
    [PostalCode] nvarchar(10) NOT NULL,
    [Country] nvarchar(50) NOT NULL,
    [InvoiceSent] bit NOT NULL,
    CONSTRAINT [PK_OrderHeaders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderHeaders_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(30) NOT NULL,
    [Brand] nvarchar(50) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ImageUrl] nvarchar(max) NULL,
    [CategoryId] int NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Promotions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(300) NULL,
    [BuyQuantity] int NOT NULL,
    [GetQuantity] int NOT NULL,
    [CategoryId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Promotions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Promotions_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Invoices] (
    [Id] int NOT NULL IDENTITY,
    [OrderHeaderId] int NULL,
    [InvoiceNumber] nvarchar(50) NOT NULL,
    [KID] nvarchar(16) NOT NULL,
    [IssueDate] date NOT NULL,
    [DueDate] date NOT NULL,
    [NetAmount] decimal(18,2) NOT NULL,
    [VatAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [Status] int NOT NULL,
    [SellerName] nvarchar(max) NOT NULL,
    [SellerOrgNumber] nvarchar(max) NOT NULL,
    [SellerAddress] nvarchar(max) NULL,
    [SellerEmail] nvarchar(max) NULL,
    [SellerPhone] nvarchar(max) NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [CustomerOrgNumber] nvarchar(max) NULL,
    [CustomerAddress] nvarchar(max) NULL,
    [CustomerEmail] nvarchar(max) NULL,
    [BankAccountNumber] nvarchar(20) NULL,
    [IBAN] nvarchar(34) NULL,
    [BIC] nvarchar(11) NULL,
    [SentDate] datetime2 NULL,
    [PdfUrl] nvarchar(max) NULL,
    [PaidDate] datetime2 NULL,
    [CancelledAt] datetime2 NULL,
    [CancelledBy] nvarchar(max) NULL,
    [CancellationReason] nvarchar(max) NULL,
    [IsBooked] bit NOT NULL,
    [BookedAt] datetime2 NULL,
    [ExternalAccountingId] nvarchar(max) NULL,
    [PeppolId] nvarchar(max) NULL,
    [IsEhfSent] bit NOT NULL,
    [EhfSentAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Invoices_OrderHeaders_OrderHeaderId] FOREIGN KEY ([OrderHeaderId]) REFERENCES [OrderHeaders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Shipments] (
    [Id] int NOT NULL IDENTITY,
    [OrderHeaderId] int NOT NULL,
    [TrackingNumber] nvarchar(max) NULL,
    [Carrier] nvarchar(max) NULL,
    [Service] nvarchar(max) NULL,
    [TrackingUrl] nvarchar(max) NULL,
    [ShippingDate] datetime2 NULL,
    [ShippedDate] datetime2 NULL,
    [DeliveredDate] datetime2 NULL,
    [ShipmentStatus] nvarchar(max) NOT NULL,
    [LabelUrl] nvarchar(max) NULL,
    [CarrierData] nvarchar(max) NULL,
    [Weight] decimal(18,2) NULL,
    [PackageType] nvarchar(max) NULL,
    CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Shipments_OrderHeaders_OrderHeaderId] FOREIGN KEY ([OrderHeaderId]) REFERENCES [OrderHeaders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ProductVariants] (
    [Id] int NOT NULL IDENTITY,
    [Color] nvarchar(30) NOT NULL,
    [SizeValueId] int NULL,
    [Price] decimal(18,2) NOT NULL,
    [Stock] int NOT NULL,
    [ProductId] int NOT NULL,
    CONSTRAINT [PK_ProductVariants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductVariants_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductVariants_SizeValues_SizeValueId] FOREIGN KEY ([SizeValueId]) REFERENCES [SizeValues] ([Id])
);
GO


CREATE TABLE [InvoicePayments] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PaymentReference] nvarchar(50) NULL,
    [PaymentMethod] int NOT NULL,
    [TransactionId] nvarchar(100) NULL,
    [IdempotencyKey] nvarchar(100) NOT NULL,
    [RegisteredBy] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_InvoicePayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoicePayments_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InvoiceLines] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [ProductVariantId] int NULL,
    [ProductName] nvarchar(200) NOT NULL,
    [ProductSku] nvarchar(50) NULL,
    [ProductDescription] nvarchar(100) NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [VatPercent] decimal(5,2) NOT NULL,
    [LineNetAmount] decimal(18,2) NOT NULL,
    [LineVatAmount] decimal(18,2) NOT NULL,
    [LineTotalAmount] decimal(18,2) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_InvoiceLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceLines_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InvoiceLines_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [OrderDetails] (
    [Id] int NOT NULL IDENTITY,
    [OrderHeaderId] int NOT NULL,
    [ProductVariantId] int NOT NULL,
    [Count] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_OrderDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderDetails_OrderHeaders_OrderHeaderId] FOREIGN KEY ([OrderHeaderId]) REFERENCES [OrderHeaders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderDetails_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Reviews] (
    [Id] int NOT NULL IDENTITY,
    [ApplicationUserId] nvarchar(450) NOT NULL,
    [ProductVariantId] int NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(500) NULL,
    [ReviewDate] datetime2 NOT NULL,
    [IsApproved] bit NOT NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reviews_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Reviews_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ShoppingCarts] (
    [Id] int NOT NULL IDENTITY,
    [ApplicationUserId] nvarchar(450) NOT NULL,
    [ProductVariantId] int NOT NULL,
    [Count] int NOT NULL,
    [DateAdded] datetime2 NULL,
    [LastUpdated] datetime2 NULL,
    CONSTRAINT [PK_ShoppingCarts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShoppingCarts_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ShoppingCarts_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ReturnRequests] (
    [Id] int NOT NULL IDENTITY,
    [OrderDetailId] int NOT NULL,
    [ApplicationUserId] nvarchar(450) NOT NULL,
    [Reason] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Quantity] int NOT NULL,
    [RequestDate] datetime2 NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [AdminNote] nvarchar(max) NULL,
    [ResolvedDate] datetime2 NULL,
    [RefundAmount] decimal(18,2) NULL,
    [RefundId] nvarchar(max) NULL,
    [RefundDate] datetime2 NULL,
    CONSTRAINT [PK_ReturnRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReturnRequests_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReturnRequests_OrderDetails_OrderDetailId] FOREIGN KEY ([OrderDetailId]) REFERENCES [OrderDetails] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CreditNotes] (
    [Id] int NOT NULL IDENTITY,
    [OriginalInvoiceId] int NOT NULL,
    [ReturnRequestId] int NULL,
    [ExternalRefundReference] nvarchar(100) NULL,
    [CreditNoteNumber] nvarchar(50) NOT NULL,
    [IssueDate] date NOT NULL,
    [Status] int NOT NULL,
    [NetAmount] decimal(18,2) NOT NULL,
    [VatAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [Reason] nvarchar(255) NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] nvarchar(100) NULL,
    [IsBooked] bit NOT NULL,
    [BookedAt] datetime2 NULL,
    [ExternalAccountingId] nvarchar(max) NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [CustomerOrgNumber] nvarchar(max) NULL,
    [CustomerAddress] nvarchar(max) NULL,
    CONSTRAINT [PK_CreditNotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CreditNotes_Invoices_OriginalInvoiceId] FOREIGN KEY ([OriginalInvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CreditNotes_ReturnRequests_ReturnRequestId] FOREIGN KEY ([ReturnRequestId]) REFERENCES [ReturnRequests] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CreditNoteLines] (
    [Id] int NOT NULL IDENTITY,
    [CreditNoteId] int NOT NULL,
    [OriginalInvoiceLineId] int NULL,
    [Description] nvarchar(200) NOT NULL,
    [ProductSku] nvarchar(50) NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [VatPercent] decimal(5,2) NOT NULL,
    [LineNetAmount] decimal(18,2) NOT NULL,
    [LineVatAmount] decimal(18,2) NOT NULL,
    [LineTotalAmount] decimal(18,2) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_CreditNoteLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CreditNoteLines_CreditNotes_CreditNoteId] FOREIGN KEY ([CreditNoteId]) REFERENCES [CreditNotes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CreditNoteLines_InvoiceLines_OriginalInvoiceLineId] FOREIGN KEY ([OriginalInvoiceLineId]) REFERENCES [InvoiceLines] ([Id]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO


CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO


CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO


CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO


CREATE INDEX [IX_AspNetUsers_CompanyId] ON [AspNetUsers] ([CompanyId]);
GO


CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO


CREATE INDEX [IX_Categories_SizeSystemId] ON [Categories] ([SizeSystemId]);
GO


CREATE INDEX [IX_CreditNoteLines_CreditNoteId] ON [CreditNoteLines] ([CreditNoteId]);
GO


CREATE INDEX [IX_CreditNoteLines_OriginalInvoiceLineId] ON [CreditNoteLines] ([OriginalInvoiceLineId]);
GO


CREATE UNIQUE INDEX [IX_CreditNotes_CreditNoteNumber] ON [CreditNotes] ([CreditNoteNumber]);
GO


CREATE INDEX [IX_CreditNotes_OriginalInvoiceId] ON [CreditNotes] ([OriginalInvoiceId]);
GO


CREATE INDEX [IX_CreditNotes_ReturnRequestId] ON [CreditNotes] ([ReturnRequestId]);
GO


CREATE INDEX [IX_InvoiceLines_InvoiceId] ON [InvoiceLines] ([InvoiceId]);
GO


CREATE INDEX [IX_InvoiceLines_ProductVariantId] ON [InvoiceLines] ([ProductVariantId]);
GO


CREATE UNIQUE INDEX [IX_InvoicePayments_IdempotencyKey] ON [InvoicePayments] ([IdempotencyKey]);
GO


CREATE INDEX [IX_InvoicePayments_InvoiceId] ON [InvoicePayments] ([InvoiceId]);
GO


CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);
GO


CREATE INDEX [IX_Invoices_OrderHeaderId] ON [Invoices] ([OrderHeaderId]);
GO


CREATE INDEX [IX_OrderDetails_OrderHeaderId] ON [OrderDetails] ([OrderHeaderId]);
GO


CREATE INDEX [IX_OrderDetails_ProductVariantId] ON [OrderDetails] ([ProductVariantId]);
GO


CREATE INDEX [IX_OrderHeaders_ApplicationUserId] ON [OrderHeaders] ([ApplicationUserId]);
GO


CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO


CREATE INDEX [IX_ProductVariants_ProductId] ON [ProductVariants] ([ProductId]);
GO


CREATE INDEX [IX_ProductVariants_SizeValueId] ON [ProductVariants] ([SizeValueId]);
GO


CREATE INDEX [IX_Promotions_CategoryId] ON [Promotions] ([CategoryId]);
GO


CREATE INDEX [IX_ReturnRequests_ApplicationUserId] ON [ReturnRequests] ([ApplicationUserId]);
GO


CREATE INDEX [IX_ReturnRequests_OrderDetailId] ON [ReturnRequests] ([OrderDetailId]);
GO


CREATE INDEX [IX_Reviews_ApplicationUserId] ON [Reviews] ([ApplicationUserId]);
GO


CREATE INDEX [IX_Reviews_ProductVariantId] ON [Reviews] ([ProductVariantId]);
GO


CREATE INDEX [IX_Shipments_OrderHeaderId] ON [Shipments] ([OrderHeaderId]);
GO


CREATE INDEX [IX_ShoppingCarts_ApplicationUserId] ON [ShoppingCarts] ([ApplicationUserId]);
GO


CREATE INDEX [IX_ShoppingCarts_ProductVariantId] ON [ShoppingCarts] ([ProductVariantId]);
GO


CREATE INDEX [IX_SizeValues_SizeSystemId] ON [SizeValues] ([SizeSystemId]);
GO


