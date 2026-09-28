-- Roles are NOT seeded by an EF HasData migration. An earlier version did that and it was
-- deliberately removed: seeded roles are a deployment concern, not a schema one.
--
-- Every statement is WHERE NOT EXISTS-guarded, so this runs safely on every Development startup.
-- The join-table column names RolesId / UsersId are EF's defaults for a skip-navigation
-- many-to-many; if you configure the join entity explicitly, update this script to match.

INSERT INTO [Authorization].[Roles] ([Id], [Name], [Description])
SELECT CAST(seed.[Id] AS uniqueidentifier), seed.[Name], seed.[Description]
FROM (VALUES
	(N'D6902B40-0001-4CAB-A40A-00037C7A0F4D', N'IT Admin', N'Full administrative access to DemoReleaseChatPlugin.'),
	(N'D6902B40-0002-4CAB-A40A-00037C7A0F4D', N'IT Operations', N'Operational access to the release management module.')
) AS seed ([Id], [Name], [Description])
WHERE NOT EXISTS (
	SELECT 1 FROM [Authorization].[Roles] existing
	WHERE existing.[Name] = seed.[Name]);

-- ---------------------------------------------------------------------------------------------
-- REPLACE THE ROW BELOW WITH YOUR OWN before first run.
--
-- Find your Entra object ID: Entra admin centre -> Users -> your account -> Object ID.
-- Or sign in once and read ClaimConstants.ObjectId from the debugger.
--
-- Without at least one seeded IT Admin, NO MENU ITEM APPEARS and every page 403s - the whole app
-- gates on that role.
-- ---------------------------------------------------------------------------------------------

INSERT INTO [Authorization].[Users] ([Id], [Name], [ExternalId])
SELECT CAST(seed.[Id] AS uniqueidentifier), seed.[Name], seed.[ExternalId]
FROM (VALUES
	(N'A1B2C3D4-0001-4E5F-8A9B-0C1D2E3F4A5B', N'Debapratim Kundu', N'cd464592-1826-4f95-83dc-93cea756d780')
) AS seed ([Id], [Name], [ExternalId])
WHERE seed.[ExternalId] NOT LIKE N'<%>'          -- skip while the placeholder is unreplaced
  AND NOT EXISTS (
	SELECT 1 FROM [Authorization].[Users] existing
	WHERE existing.[ExternalId] = seed.[ExternalId]);

INSERT INTO [Authorization].[UserRoles] ([RolesId], [UsersId])
SELECT r.[Id], u.[Id]
FROM [Authorization].[Roles] r
CROSS JOIN [Authorization].[Users] u
WHERE r.[Name] = N'IT Admin'
  AND u.[ExternalId] NOT LIKE N'<%>'
  AND NOT EXISTS (
	SELECT 1 FROM [Authorization].[UserRoles] existing
	WHERE existing.[RolesId] = r.[Id] AND existing.[UsersId] = u.[Id]);
