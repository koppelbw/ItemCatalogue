-- Seed_Room.sql
-- Each Room belongs to a Floor (Room.FloorId). Floor seeds run first (see Script.PostDeployment.sql).
-- Geometry is in inches; RoomType/colours are illustrative. Rotation is 0 (axis-aligned footprints).

SET IDENTITY_INSERT dbo.Room ON;

MERGE INTO dbo.Room AS target
USING (VALUES
    -- Id, Name,         Description,            FloorId, RoomType,     OX,    OY,  Width, Depth, Height, Rot,  WallColor,  FloorColor
    -- Apartment (Floor 1): 2 bed / 1 bath, ~1,000 sq ft. Layout digitised from the floor plan:
    -- bedrooms stacked on the west side (their closets are Wardrobe containers inside the rooms,
    -- not separate rooms) with a hallway between them and the kitchen; entry hall along the north
    -- opens into the living room; open living/dining on the east with a balcony beyond.
    (1,  'Bedroom 1',   'Guest bedroom (front)', 1, 'Bedroom',      0.00,  48.00, 180.00, 144.00,  96.00, 0.00, '#E8E0D5', '#B8A98F'),
    (2,  'Living Room', 'Open living/dining area', 1, 'LivingRoom', 390.00,  0.00, 204.00, 216.00,  96.00, 0.00, '#F2EEE8', '#B8A98F'),
    -- House (Floor 4 'Main' + Floor 6 'Attic'): single story, digitised from the hand-drawn plan.
    -- Bedroom wing on the west, kitchen/baths/laundry along a central hallway, sun room and patio
    -- deck to the north, art studio + tool shop north-east, living room / piano room / garage on
    -- the south-east. Closets are Wardrobe containers inside their rooms. Room 7 (Basement) is
    -- retired; Seed_Cleanup.sql deletes it.
    -- Room 3 is Grandmas' first-floor kitchen (Floor 2); repositioned for the townhouse layout below.
    (3,  'Kitchen',     'First-floor kitchen',   2, 'Kitchen',     60.00, 120.00, 156.00, 120.00,  96.00, 0.00, '#F5F1E8', '#D8CBB3'),
    (4,  'Art Studio',  'Painting studio',       4, 'Office',     378.00,  30.00,  96.00, 174.00,  96.00, 0.00, '#FFFFFF', '#D8CBB3'),
    (5,  'Garage',      'Attached garage',       4, 'Garage',     402.00, 486.00, 150.00, 108.00,  96.00, 0.00, '#C0C0C0', '#808080'),
    (6,  'Storage',     'Storage room',          7, 'Other',        0.00,   0.00,  96.00,  96.00, 120.00, 0.00, '#D3D3D3', '#A9A9A9'),
    (8,  'Attic',       'Small attic (hatch access)', 6, 'Attic',   0.00,   0.00, 240.00, 180.00,  48.00, 0.00, '#C8B89A', '#8B7355'),
    (9,  'Bathroom',    'Main bathroom',         4, 'Bathroom',   120.00, 210.00,  66.00,  96.00,  96.00, 0.00, '#E0F7FA', '#B0BEC5'),
    (10, 'Kitchen',     'Kitchen',               4, 'Kitchen',    192.00, 210.00, 156.00,  96.00,  96.00, 0.00, '#F5F1E8', '#D8CBB3'),
    -- Car (Floor 8): a single room for the whole vehicle; the glove box and trunk are Box/Chest
    -- containers inside it (see Seed_Container.sql). Room 12 (the old separate Trunk room) is retired;
    -- Seed_Cleanup.sql deletes it.
    (11, 'Car',         'Subaru Forester interior & cargo', 8, 'Other', 0.00, 0.00, 72.00, 180.00, 54.00, 0.00, '#8D9199', '#63666C'),
    -- Apartment rooms (Floor 1), continued from rooms 1-2 above
    (13, 'Bedroom 2',        'Primary bedroom',             1, 'Bedroom',    0.00, 198.00, 180.00, 150.00, 96.00, 0.00, '#E8E0D5', '#B8A98F'),
    (14, 'Kitchen',          'Galley kitchen with island',  1, 'Kitchen',  234.00,  96.00, 150.00, 120.00, 96.00, 0.00, '#F5F1E8', '#D8CBB3'),
    (15, 'Bathroom',         'Full bathroom',               1, 'Bathroom', 234.00, 222.00,  96.00,  90.00, 96.00, 0.00, '#E0F0F0', '#CFD8DC'),
    (16, 'Laundry Room',     'Washer/dryer room',           1, 'Laundry',  336.00, 222.00,  84.00,  90.00, 96.00, 0.00, '#F0F0F0', '#CFD8DC'),
    (17, 'Hallway',          'Bedroom-wing hallway',        1, 'Hallway',  186.00,  96.00,  42.00, 216.00, 96.00, 0.00, '#EFE9E0', '#B8A98F'),
    (18, 'Entry Hall',       'Entry, opens into the living room', 1, 'Hallway', 186.00, 0.00, 204.00, 90.00, 96.00, 0.00, '#EFE9E0', '#B8A98F'),
    -- Ids 19-23 (closet/utility rooms from an earlier layout) are retired; Seed_Cleanup.sql deletes them.
    (24, 'Balcony',          'Private balcony',             1, 'Other',    600.00,  24.00,  48.00, 168.00, 96.00, 0.00, '#D9CDBA', '#8A8D91'),
    -- House rooms (Floor 4), continued from rooms 4-5 and 8-10 above
    (25, 'Bed 1',       'Bedroom 1 (front, west wing)', 4, 'Bedroom',      0.00, 210.00, 114.00,  96.00, 96.00, 0.00, '#E8E0D5', '#B8A98F'),
    (26, 'Bed 2',       'Bedroom 2 (south-west)',       4, 'Bedroom',      0.00, 354.00,  90.00,  96.00, 96.00, 0.00, '#E8E0D5', '#B8A98F'),
    (27, 'Bed 3',       'Bedroom 3 (south)',            4, 'Bedroom',     96.00, 354.00,  90.00,  96.00, 96.00, 0.00, '#E8E0D5', '#B8A98F'),
    (28, 'Bed 4',       'Bedroom 4 (primary, east)',    4, 'Bedroom',    444.00, 210.00, 108.00, 138.00, 96.00, 0.00, '#E8E0D5', '#B8A98F'),
    (29, 'Half Bath',   'Powder room off the hallway',  4, 'Bathroom',   354.00, 210.00,  36.00,  96.00, 96.00, 0.00, '#E0F7FA', '#B0BEC5'),
    (30, 'Laundry',     'Laundry room',                 4, 'Laundry',    396.00, 210.00,  42.00,  96.00, 96.00, 0.00, '#F0F0F0', '#CFD8DC'),
    (31, 'Sun Room',    'Sun room off the kitchen',     4, 'Other',      192.00, 120.00,  84.00,  84.00, 96.00, 0.00, '#FFF8E1', '#C8A165'),
    (32, 'Patio Deck',  'Outdoor patio deck',           4, 'Other',      282.00,  24.00,  90.00, 180.00, 96.00, 0.00, '#B08968', '#9C7A54'),
    (33, 'Tool Shop',   'Tool shop off the art studio', 4, 'Other',      480.00, 120.00,  72.00,  84.00, 96.00, 0.00, '#E8E8E8', '#9E9E9E'),
    (34, 'Hallway',     'Central hallway',              4, 'Hallway',      0.00, 312.00, 438.00,  36.00, 96.00, 0.00, '#EFE9E0', '#B8A98F'),
    (35, 'Living Room', 'Living room',                  4, 'LivingRoom', 192.00, 354.00, 204.00, 105.00, 96.00, 0.00, '#F2EEE8', '#8B6F4E'),
    (36, 'Piano Room',  'Piano room',                   4, 'Other',      402.00, 354.00, 150.00, 120.00, 96.00, 0.00, '#F2EEE8', '#8B6F4E'),
    -- Grandmas townhouse (Location 2). 216 (X) x 360 (Y) footprint per floor; a 60-wide stair hall
    -- runs down the west side (X 0-60) on every level and holds the stairs, with rooms to the east.
    -- Floors: 9 Basement, 2 First, 10 Second, 11 Third, 12 Attic. Room 3 (Kitchen) is on Floor 2.
    (37, 'Stair Hall', 'Basement stair hall', 9, 'Hallway', 0.00, 0.00, 60.00, 360.00, 84.00, 0.00, '#D8D2C4', '#9E9E9E'),
    (38, 'Utility Room', 'Furnace, water heater and workbench', 9, 'Other', 60.00, 0.00, 156.00, 120.00, 84.00, 0.00, '#CFCFCF', '#8A8A8A'),
    (39, 'Rec Room', 'Basement rec room', 9, 'LivingRoom', 60.00, 120.00, 156.00, 130.00, 84.00, 0.00, '#E6E0D4', '#8B7355'),
    (40, 'Basement Storage', 'Basement storage', 9, 'Other', 60.00, 250.00, 156.00, 110.00, 84.00, 0.00, '#D3D3D3', '#A9A9A9'),
    (41, 'Foyer', 'Entry foyer with the front door', 2, 'Hallway', 0.00, 0.00, 60.00, 360.00, 96.00, 0.00, '#EFE9E0', '#B8A98F'),
    (42, 'Living Room', 'Front living room', 2, 'LivingRoom', 60.00, 0.00, 156.00, 120.00, 96.00, 0.00, '#F2EEE8', '#A0785A'),
    (43, 'Dining Room', 'Dining room', 2, 'DiningRoom', 60.00, 240.00, 96.00, 120.00, 96.00, 0.00, '#FFF8E1', '#8D6E63'),
    (44, 'Powder Room', 'Powder room off the dining room', 2, 'Bathroom', 156.00, 240.00, 60.00, 120.00, 96.00, 0.00, '#E0F0F0', '#CFD8DC'),
    (45, 'Landing (2nd)', 'Second-floor landing', 10, 'Hallway', 0.00, 0.00, 60.00, 360.00, 96.00, 0.00, '#EFE9E0', '#B8A98F'),
    (46, 'Primary Bedroom', 'Primary bedroom', 10, 'Bedroom', 60.00, 0.00, 156.00, 150.00, 96.00, 0.00, '#E8E0D5', '#B8A98F'),
    (47, 'Main Bathroom', 'Shared main bathroom', 10, 'Bathroom', 60.00, 150.00, 156.00, 90.00, 96.00, 0.00, '#E0F7FA', '#B0BEC5'),
    (48, 'Bedroom 2', 'Second bedroom', 10, 'Bedroom', 60.00, 240.00, 156.00, 120.00, 96.00, 0.00, '#E8E0D5', '#B8A98F'),
    (49, 'Landing (3rd)', 'Third-floor landing', 11, 'Hallway', 0.00, 0.00, 60.00, 360.00, 96.00, 0.00, '#EFE9E0', '#B8A98F'),
    (50, 'Bedroom 3', 'Third bedroom', 11, 'Bedroom', 60.00, 0.00, 156.00, 130.00, 96.00, 0.00, '#E8E0D5', '#B8A98F'),
    (51, 'Study', 'Study / home office', 11, 'Office', 60.00, 130.00, 156.00, 110.00, 96.00, 0.00, '#DDE6ED', '#6B4423'),
    (52, 'Bathroom', 'Third-floor bathroom', 11, 'Bathroom', 60.00, 240.00, 78.00, 120.00, 96.00, 0.00, '#E0F7FA', '#B0BEC5'),
    (53, 'Linen Closet', 'Linen closet', 11, 'Closet', 138.00, 240.00, 78.00, 120.00, 96.00, 0.00, '#F5F5F5', '#B8A98F'),
    (54, 'Attic Landing', 'Attic landing', 12, 'Hallway', 0.00, 0.00, 60.00, 360.00, 72.00, 0.00, '#D8D2C4', '#8B7355'),
    (55, 'Attic', 'Attic storage under the eaves', 12, 'Attic', 60.00, 0.00, 156.00, 360.00, 72.00, 0.00, '#C8B89A', '#8B7355')
) AS source (Id, Name, Description, FloorId, RoomType, OriginXInches, OriginYInches, WidthInches, DepthInches, HeightInches, Rotation, WallColor, FloorColor)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Description = source.Description,
    FloorId = source.FloorId,
    RoomType = source.RoomType,
    OriginXInches = source.OriginXInches,
    OriginYInches = source.OriginYInches,
    WidthInches = source.WidthInches,
    DepthInches = source.DepthInches,
    HeightInches = source.HeightInches,
    Rotation = source.Rotation,
    WallColor = source.WallColor,
    FloorColor = source.FloorColor
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description, FloorId, RoomType, OriginXInches, OriginYInches, WidthInches, DepthInches, HeightInches, Rotation, WallColor, FloorColor)
    VALUES (source.Id, source.Name, source.Description, source.FloorId, source.RoomType, source.OriginXInches, source.OriginYInches, source.WidthInches, source.DepthInches, source.HeightInches, source.Rotation, source.WallColor, source.FloorColor);

SET IDENTITY_INSERT dbo.Room OFF;
