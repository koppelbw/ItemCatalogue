-- Seed_Door.sql
-- Doors reference Rooms (Door.FromRoomId required; Door.ToRoomId NULL = leads outside), so Room
-- seeds run first (see Script.PostDeployment.sql). Geometry is in inches, relative to FromRoom's wall.

SET IDENTITY_INSERT dbo.Door ON;

MERGE INTO dbo.Door AS target
USING (VALUES
    -- Id, Name,              Kind,      FromRoomId, ToRoomId, Wall,    Offset, Width, Height
    -- Apartment doors (rooms 1-2, 13-24)
    (1,  'Front Door',            'Door',        18,    NULL, 'North', 60.00, 36.00, 80.00),  -- Entry Hall -> outside
    (2,  'Bedroom 1 Door',        'Door',         1,      17, 'East', 108.00, 32.00, 80.00),  -- Bedroom 1 <-> Hallway
    (3,  'Bathroom Door',         'Door',         9,      34, 'South', 22.00, 28.00, 80.00),  -- House: Bathroom <-> Hallway
    (4,  'Garage Door',           'Garage',       5,    NULL, 'South', 21.00, 108.00, 84.00), -- House: Garage -> driveway
    (5,  'Bedroom 2 Door',        'Door',        13,      17, 'East',  24.00, 32.00, 80.00),  -- Bedroom 2 <-> Hallway
    (6,  'Bathroom Door',         'Door',        15,      17, 'West',  30.00, 28.00, 80.00),  -- Bathroom <-> Hallway
    (7,  'Hallway Opening',       'Doorway',     17,      18, 'North',  3.00, 36.00, 80.00),  -- Hallway <-> Entry Hall
    (8,  'Kitchen Entry Opening', 'Doorway',     14,      18, 'North', 12.00, 48.00, 80.00),  -- Kitchen <-> Entry Hall
    (9,  'Kitchen Pass-Through',  'Doorway',     14,       2, 'East',  12.00, 96.00, 80.00),  -- Kitchen <-> Living Room (open to dining)
    (10, 'Balcony Slider',        'SlidingDoor',  2,      24, 'East',  84.00, 72.00, 80.00),  -- Living Room <-> Balcony
    (11, 'Entry Opening',         'Doorway',     18,       2, 'East',  18.00, 60.00, 80.00),  -- Entry Hall <-> Living Room
    -- Ids 12-15 (closet doors from an earlier layout) are retired; Seed_Cleanup.sql deletes them.
    (16, 'Laundry Door',          'Door',        16,       2, 'North', 56.00, 30.00, 80.00),  -- Laundry <-> Living/Dining
    -- House doors (rooms 4-5, 8-10, 25-36); red marks on the hand-drawn plan
    (17, 'Front Door',            'Door',        35,    NULL, 'South', 96.00, 36.00, 80.00),  -- Living Room -> outside
    (18, 'Bed 1 Door',            'Door',        25,      34, 'South', 78.00, 32.00, 80.00),  -- Bed 1 <-> Hallway
    (19, 'Bed 2 Door',            'Door',        26,      34, 'North', 52.00, 32.00, 80.00),  -- Bed 2 <-> Hallway
    (20, 'Bed 3 Door',            'Door',        27,      34, 'North', 40.00, 32.00, 80.00),  -- Bed 3 <-> Hallway
    (21, 'Bed 4 Door',            'Door',        28,      34, 'West', 106.00, 30.00, 80.00),  -- Bed 4 <-> Hallway (east end)
    (22, 'Kitchen Opening',       'Doorway',     10,      34, 'South', 60.00, 72.00, 80.00),  -- Kitchen <-> Hallway (wide opening)
    (23, 'Half Bath Door',        'Door',        29,      34, 'South',  4.00, 28.00, 80.00),  -- Half Bath <-> Hallway
    (24, 'Laundry Room Door',     'Door',        30,      34, 'South',  7.00, 28.00, 80.00),  -- Laundry <-> Hallway
    (25, 'Sun Room Doorway',      'Doorway',     31,      10, 'South', 24.00, 48.00, 80.00),  -- Sun Room <-> Kitchen
    (26, 'Kitchen Patio Door',    'Door',        10,      32, 'North',126.00, 28.00, 80.00),  -- Kitchen <-> Patio Deck
    (27, 'Studio Patio Door',     'Door',         4,      32, 'West',  84.00, 36.00, 80.00),  -- Art Studio <-> Patio Deck
    (28, 'Studio Laundry Door',   'Door',        30,       4, 'North',  7.00, 28.00, 80.00),  -- Laundry <-> Art Studio
    (29, 'Tool Shop Door',        'Door',        33,       4, 'West',  24.00, 32.00, 80.00),  -- Tool Shop <-> Art Studio
    (30, 'Living Room Opening',   'Doorway',     35,      34, 'North', 84.00, 60.00, 80.00),  -- Living Room <-> Hallway
    (31, 'Piano Room Doorway',    'Doorway',     36,      34, 'North',  3.00, 30.00, 80.00),  -- Piano Room <-> Hallway
    (32, 'Living-Piano Opening',  'Doorway',     35,      36, 'East',  60.00, 40.00, 80.00),  -- Living Room <-> Piano Room
    (33, 'Piano-Garage Door',     'Door',        36,       5, 'South', 78.00, 32.00, 80.00),  -- Piano Room <-> Garage
    -- Id 34 (Piano Side Door) is retired; Seed_Cleanup.sql deletes it.
    (35, 'Sun Room Patio Door',   'Door',        31,      32, 'East',  24.00, 36.00, 80.00),  -- Sun Room <-> Patio Deck
    -- Grandmas townhouse (rooms 3, 37-55): front door + one door per room off the stair hall on each
    -- floor (hall is on the west, so its East wall opens into the rooms; offset = room's Y along the hall).
    (36, 'Front Door', 'Door', 41, NULL, 'North', 20.00, 36.00, 80.00),        -- Foyer -> outside
    (37, 'Utility Door', 'Door', 37, 38, 'East', 40.00, 32.00, 80.00),         -- Basement hall <-> Utility
    (38, 'Rec Room Opening', 'Doorway', 37, 39, 'East', 170.00, 48.00, 80.00), -- Basement hall <-> Rec Room
    (39, 'Storage Door', 'Door', 37, 40, 'East', 290.00, 32.00, 80.00),        -- Basement hall <-> Storage
    (40, 'Living Room Opening', 'Doorway', 41, 42, 'East', 40.00, 60.00, 80.00), -- Foyer <-> Living Room
    (41, 'Kitchen Door', 'Door', 41, 3, 'East', 170.00, 32.00, 80.00),         -- Foyer <-> Kitchen
    (42, 'Dining Room Opening', 'Doorway', 41, 43, 'East', 290.00, 48.00, 80.00), -- Foyer <-> Dining Room
    (43, 'Powder Room Door', 'Door', 43, 44, 'East', 40.00, 28.00, 80.00),     -- Dining Room <-> Powder Room
    (44, 'Primary Bedroom Door', 'Door', 45, 46, 'East', 60.00, 32.00, 80.00), -- 2nd landing <-> Primary
    (45, 'Bathroom Door', 'Door', 45, 47, 'East', 190.00, 28.00, 80.00),       -- 2nd landing <-> Main Bath
    (46, 'Bedroom 2 Door', 'Door', 45, 48, 'East', 290.00, 32.00, 80.00),      -- 2nd landing <-> Bedroom 2
    (47, 'Bedroom 3 Door', 'Door', 49, 50, 'East', 60.00, 32.00, 80.00),       -- 3rd landing <-> Bedroom 3
    (48, 'Study Door', 'Door', 49, 51, 'East', 180.00, 32.00, 80.00),          -- 3rd landing <-> Study
    (49, 'Third Bath Door', 'Door', 49, 52, 'East', 290.00, 28.00, 80.00),     -- 3rd landing <-> Bathroom
    (50, 'Linen Closet Door', 'Door', 49, 53, 'East', 300.00, 24.00, 80.00),   -- 3rd landing <-> Linen Closet
    (51, 'Attic Door', 'Door', 54, 55, 'East', 40.00, 30.00, 72.00)            -- Attic landing <-> Attic
) AS source (Id, Name, Kind, FromRoomId, ToRoomId, Wall, OffsetInches, WidthInches, HeightInches)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Kind = source.Kind,
    FromRoomId = source.FromRoomId,
    ToRoomId = source.ToRoomId,
    Wall = source.Wall,
    OffsetInches = source.OffsetInches,
    WidthInches = source.WidthInches,
    HeightInches = source.HeightInches
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Kind, FromRoomId, ToRoomId, Wall, OffsetInches, WidthInches, HeightInches)
    VALUES (source.Id, source.Name, source.Kind, source.FromRoomId, source.ToRoomId, source.Wall, source.OffsetInches, source.WidthInches, source.HeightInches);

SET IDENTITY_INSERT dbo.Door OFF;
