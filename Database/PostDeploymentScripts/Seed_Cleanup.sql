-- Seed_Cleanup.sql
-- Removes seed rows that earlier versions of these scripts created and the current layout no longer
-- uses (the MERGE seeds only upsert; they never delete). Runs LAST so the preceding seeds have
-- already re-pointed containers/items/doors away from the retired rooms. Idempotent: deletes by Id
-- and no-ops when the rows do not exist. Seeded ids stay below the identity high-water mark, so
-- these ids can never belong to user-created rows.

-- Closet doors from the closet-rooms layout, plus the retired piano-room side door (34).
-- (Doors reference rooms, so they go before any room delete.)
DELETE FROM dbo.Door WHERE Id IN (12, 13, 14, 15, 34);

-- (Stair 1 is no longer retired: it is now Grandmas' basement stair, seeded in Seed_Stair.sql.)

-- Apartment: bedroom 1 / linen / bedroom 2 / coat / utility closet rooms; closets are now Wardrobe
-- containers inside their parent rooms (see Seed_Container.sql).
-- House: the basement room (7) went away with the single-story layout.
-- Car: the separate Trunk room (12) was merged into the single 'Car' room (11); glove box and trunk
-- are now containers inside it.
DELETE FROM dbo.Room WHERE Id IN (7, 12, 19, 20, 21, 22, 23);

-- House floors: Basement (3) and Second Floor (5); rooms above referenced them, so floors go last.
DELETE FROM dbo.Floor WHERE Id IN (3, 5);
