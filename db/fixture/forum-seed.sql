-- Forum categories for the test fixture. HAND-WRITTEN, unlike fixture.sql beside it.
--
-- db/make-fixture.py takes an anonymised slice of the real database, and it covers the five
-- tables the rating pipeline needs. The forum is not among them, so a freshly built test
-- database has no categories at all - and the site's front page is a forum page.
--
-- That gap was invisible for as long as the host harness only ever ran on a laptop, where the
-- test database had accumulated categories from earlier work. Its first run in CI, against a
-- database built from nothing, threw "Sequence contains no elements" on the first query it
-- made. This is the fix, and it is synthetic rather than generated because there is nothing
-- real to anonymise here: two rows and a name.
--
-- Loaded by db/load-fixture.sh after fixture.sql.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DELETE FROM [dbo].[ForumCategories];

SET IDENTITY_INSERT [dbo].[ForumCategories] ON;

-- A parent and a child, because the harness looks for the deepest category it can find and
-- a single flat one would not exercise the same query.
INSERT INTO [dbo].[ForumCategories]
    ([ForumCategoryID], [Title], [ParentForumCategoryID], [IsLocked], [SortOrder], [ForumMode])
VALUES
    (1, N'General', NULL, 0, 0, 0),
    (2, N'Help and bugs', 1, 0, 1, 0);

SET IDENTITY_INSERT [dbo].[ForumCategories] OFF;
