-- Seed_Container.sql
-- A Container is owned by exactly one parent: RoomId (top-level) XOR ParentContainerId (nested).
-- Location/Floor/Room seeds run first; parent containers are listed before the children that
-- reference them (the self-referencing FK is NO ACTION, checked at statement end, so one MERGE is
-- fine). Placement is in inches: top-level containers are positioned in room space, nested ones in
-- parent-container space. Rotation is 0.

SET IDENTITY_INSERT dbo.Container ON;

MERGE INTO dbo.Container AS target
USING (VALUES
    -- Id, Name,         Description,             RoomId, ParentId, Type,       PosX,  PosY, PosZ,  Rot,  Width, Depth, Height, Color
    -- Top-level containers (RoomId set, ParentContainerId NULL)
    (1, 'Dresser',          'Bedroom 1 dresser',                    1, NULL, 'Cabinet',   8.00, 118.00, 0.00, 0.00,  60.00, 20.00, 34.00, '#8B5A2B'),
    (2, 'Closet',           'Bedroom 1 closet (wardrobe)',          1, NULL, 'Wardrobe', 152.00,  8.00, 0.00, 0.00,  26.00, 72.00, 84.00, '#D2B48C'),
    (3, 'Desk',             'Art studio desk',                      4, NULL, 'Cabinet',   8.00,  48.00, 0.00, 0.00,  60.00, 30.00, 30.00, '#6B4423'),
    (4, 'Cabinet',          'Garage storage cabinet',               5, NULL, 'Cabinet',  10.00,  10.00, 0.00, 0.00,  36.00, 18.00, 72.00, '#C0C0C0'),
    -- Nested containers (ParentContainerId set, RoomId NULL)
    (5, 'Storage Bin', 'Bin inside the closet', NULL,      2, 'Bin',       2.00,  2.00, 6.00, 0.00, 18.00, 14.00, 12.00, '#4682B4'),
    (6, 'Box',         'Box inside the bin',    NULL,      5, 'Box',       1.00,  1.00, 0.00, 0.00,  8.00,  6.00,  6.00, '#DEB887'),
    -- Apartment furniture (matches the floor plan; positions are in room space, inches)
    -- Bedroom 1 (room 1, 180x144)
    (7,  'Guest Bed',             'Full bed with under-bed storage',      1, NULL, 'Other',    36.00,   6.00,  0.00, 0.00,  60.00, 80.00, 22.00, '#8B7355'),
    (8,  'Bookshelf',             'Small bookshelf',                      1, NULL, 'Shelf',   100.00, 122.00,  0.00, 0.00,  30.00, 12.00, 48.00, '#6B4423'),
    -- Bedroom 2 / primary (room 13, 180x150)
    (9,  'Queen Bed',             'Queen bed with under-bed storage',    13, NULL, 'Other',     8.00,  45.00,  0.00, 0.00,  80.00, 60.00, 24.00, '#7A5C44'),
    (10, 'Nightstand (Left)',     'Nightstand left of the bed',          13, NULL, 'Cabinet',   8.00,  24.00,  0.00, 0.00,  18.00, 16.00, 24.00, '#7A5C44'),
    (11, 'Nightstand (Right)',    'Nightstand right of the bed',         13, NULL, 'Cabinet',   8.00, 110.00,  0.00, 0.00,  18.00, 16.00, 24.00, '#7A5C44'),
    (12, 'Master Dresser',        'Dresser on the south wall',           13, NULL, 'Cabinet',  40.00, 128.00,  0.00, 0.00,  60.00, 20.00, 34.00, '#7A5C44'),
    -- Kitchen (room 14, 150x120)
    (13, 'Kitchen Base Cabinets', 'Base cabinet run with counter',       14, NULL, 'Cabinet',   0.00,  96.00,  0.00, 0.00, 144.00, 24.00, 36.00, '#C8A165'),
    (14, 'Kitchen Upper Cabinets','Wall cabinets above the counter',     14, NULL, 'Cabinet',   0.00, 108.00, 54.00, 0.00, 144.00, 12.00, 30.00, '#C8A165'),
    (15, 'Kitchen Island',        'Island / breakfast bar',              14, NULL, 'Cabinet',  24.00,  40.00,  0.00, 0.00,  84.00, 24.00, 36.00, '#C8A165'),
    (16, 'Refrigerator',          'Fridge/freezer',                      14, NULL, 'Other',   120.00,   4.00,  0.00, 0.00,  24.00, 30.00, 68.00, '#DCDCDC'),
    -- Closets (wardrobe containers inside their rooms; there are no separate closet rooms)
    (17, 'Coat Closet',           'Coat closet by the front door',       18, NULL, 'Wardrobe', 154.00,   4.00,  0.00, 0.00,  44.00, 22.00, 84.00, '#D2B48C'),
    (18, 'Linen Cabinet',         'Linen storage in the bathroom',       15, NULL, 'Cabinet',  70.00,   4.00,  0.00, 0.00,  24.00, 16.00, 66.00, '#FFFFFF'),
    (19, 'Master Closet',         'Bedroom 2 closet (wardrobe)',         13, NULL, 'Wardrobe', 152.00,  70.00,  0.00, 0.00,  26.00, 76.00, 84.00, '#D2B48C'),
    -- Living / dining (room 2, 204x216)
    (20, 'Media Console',         'TV console on the north wall',         2, NULL, 'Cabinet',  60.00,   4.00,  0.00, 0.00,  60.00, 18.00, 24.00, '#4A3728'),
    (21, 'Sofa',                  'Three-seat sofa',                      2, NULL, 'Other',    66.00, 132.00,  0.00, 0.00,  84.00, 36.00, 34.00, '#9CAF88'),
    (22, 'Coffee Table',          'Coffee table with storage shelf',      2, NULL, 'Other',    88.00,  96.00,  0.00, 0.00,  40.00, 24.00, 18.00, '#4A3728'),
    (23, 'Dining Table',          'Four-seat dining table',               2, NULL, 'Other',    18.00, 120.00,  0.00, 0.00,  42.00, 42.00, 30.00, '#4A3728'),
    -- Bathroom (room 15, 96x90)
    (24, 'Bathroom Vanity',       'Sink vanity with cabinet',            15, NULL, 'Cabinet',   4.00,   4.00,  0.00, 0.00,  30.00, 21.00, 32.00, '#FFFFFF'),
    (25, 'Medicine Cabinet',      'Wall-mounted medicine cabinet',       15, NULL, 'Cabinet',  40.00,   0.00, 48.00, 0.00,  20.00,  5.00, 28.00, '#FFFFFF'),
    -- Laundry (room 16, 84x90)
    (26, 'Laundry Shelf',         'Wall shelf above the machines',       16, NULL, 'Shelf',     4.00,   4.00, 48.00, 0.00,  48.00, 12.00, 12.00, '#FFFFFF'),
    -- Balcony (room 24, 48x168)
    (27, 'Balcony Storage Bench', 'Weatherproof deck box',               24, NULL, 'Chest',     4.00, 110.00,  0.00, 0.00,  40.00, 18.00, 20.00, '#5C5C5C'),
    -- Entry hall (room 18, 204x90)
    (28, 'Entry Shoe Rack',       'Shoe rack by the front door',         18, NULL, 'Shelf',    10.00,  72.00,  0.00, 0.00,  36.00, 14.00, 24.00, '#6B4423'),
    -- House furniture (rooms 4-5, 8-10, 25-36); green strips on the plan are the wardrobe closets
    -- Bed 1 (room 25, 114x96)
    (29, 'Bed 1 Closet',          'Bed 1 closet (wardrobe)',             25, NULL, 'Wardrobe', 94.00,   8.00,  0.00, 0.00,  20.00, 64.00, 84.00, '#D2B48C'),
    (30, 'Bed 1 Bed',             'Full bed with under-bed storage',     25, NULL, 'Other',     6.00,  34.00,  0.00, 0.00,  75.00, 54.00, 22.00, '#8B7355'),
    (31, 'Bed 1 Dresser',         'Dresser',                             25, NULL, 'Cabinet',   6.00,   4.00,  0.00, 0.00,  42.00, 18.00, 34.00, '#8B5A2B'),
    -- Main bathroom (room 9, 66x96)
    (32, 'Bathroom Vanity',       'Sink vanity with cabinet',             9, NULL, 'Cabinet',   4.00,   4.00,  0.00, 0.00,  36.00, 21.00, 32.00, '#FFFFFF'),
    (33, 'Bath Linen Cabinet',    'Tall linen cabinet',                   9, NULL, 'Cabinet',  44.00,  28.00,  0.00, 0.00,  20.00, 34.00, 72.00, '#FFFFFF'),
    -- Kitchen (room 10, 156x96)
    (34, 'Kitchen Base Cabinets', 'Base cabinet run with counter',       10, NULL, 'Cabinet',   0.00,   0.00,  0.00, 0.00,  24.00, 96.00, 36.00, '#C8A165'),
    (35, 'Kitchen Upper Cabinets','Wall cabinets above the counter',     10, NULL, 'Cabinet',   0.00,   0.00, 54.00, 0.00,  12.00, 96.00, 30.00, '#C8A165'),
    (36, 'Refrigerator',          'Fridge/freezer',                      10, NULL, 'Other',    74.00,   2.00,  0.00, 0.00,  24.00, 30.00, 68.00, '#DCDCDC'),
    -- Half bath (room 29, 36x96)
    (37, 'Powder Room Vanity',    'Small sink vanity',                   29, NULL, 'Cabinet',   4.00,   4.00,  0.00, 0.00,  24.00, 18.00, 32.00, '#FFFFFF'),
    -- Laundry (room 30, 42x96)
    (38, 'Laundry Wall Shelf',    'Shelf above the machines',            30, NULL, 'Shelf',    30.00,  31.00, 48.00, 0.00,  12.00, 34.00, 12.00, '#FFFFFF'),
    -- Bed 4 / primary (room 28, 108x138)
    (39, 'Bed 4 Closet',          'Bed 4 closet (wardrobe)',             28, NULL, 'Wardrobe',  4.00,   8.00,  0.00, 0.00,  18.00, 90.00, 84.00, '#D2B48C'),
    (40, 'Bed 4 Bed',             'Queen bed with under-bed storage',    28, NULL, 'Other',    40.00,   8.00,  0.00, 0.00,  60.00, 80.00, 24.00, '#7A5C44'),
    (41, 'Bed 4 Nightstand',      'Nightstand',                          28, NULL, 'Cabinet',  22.00,   8.00,  0.00, 0.00,  18.00, 16.00, 24.00, '#7A5C44'),
    -- Bed 2 (room 26, 90x96)
    (42, 'Bed 2 Closet',          'Bed 2 closet (wardrobe)',             26, NULL, 'Wardrobe',  4.00,   4.00,  0.00, 0.00,  44.00, 16.00, 84.00, '#D2B48C'),
    (43, 'Bed 2 Bed',             'Twin bed with under-bed storage',     26, NULL, 'Other',     4.00,  21.00,  0.00, 0.00,  38.00, 75.00, 22.00, '#8B7355'),
    (44, 'Bed 2 Desk',            'Kids desk',                           26, NULL, 'Cabinet',  70.00,  44.00,  0.00, 0.00,  20.00, 40.00, 30.00, '#8B5A2B'),
    -- Bed 3 (room 27, 90x96)
    (45, 'Bed 3 Closet',          'Bed 3 closet (wardrobe)',             27, NULL, 'Wardrobe', 72.00,  26.00,  0.00, 0.00,  18.00, 44.00, 84.00, '#D2B48C'),
    (46, 'Bed 3 Bed',             'Twin bed with under-bed storage',     27, NULL, 'Other',     2.00,  21.00,  0.00, 0.00,  38.00, 75.00, 22.00, '#8B7355'),
    (47, 'Bed 3 Bookshelf',       'Bookshelf',                           27, NULL, 'Shelf',     2.00,   4.00,  0.00, 0.00,  30.00, 12.00, 48.00, '#6B4423'),
    -- Living room (room 35, 204x105)
    (48, 'Hall Storage Closet',   'Storage closet (west wall)',          35, NULL, 'Wardrobe',  2.00,   8.00,  0.00, 0.00,  20.00, 60.00, 84.00, '#D2B48C'),
    (49, 'Sofa',                  'Three-seat sofa',                     35, NULL, 'Other',     8.00,  66.00,  0.00, 0.00,  84.00, 36.00, 34.00, '#7B6D8D'),
    (50, 'TV Console',            'TV console on the north wall',        35, NULL, 'Cabinet', 144.00,   4.00,  0.00, 0.00,  54.00, 18.00, 24.00, '#4A3728'),
    (51, 'Coffee Table',          'Coffee table with storage shelf',     35, NULL, 'Other',    30.00,  34.00,  0.00, 0.00,  40.00, 24.00, 18.00, '#4A3728'),
    -- Piano room (room 36, 150x120)
    (52, 'Grand Piano',           'Baby grand piano; the bench stores music',36, NULL, 'Other',40.00,   4.00,  0.00, 0.00,  60.00, 58.00, 40.00, '#2B1B12'),
    (53, 'Sheet Music Cabinet',   'Sheet music cabinet',                 36, NULL, 'Cabinet',   8.00, 100.00,  0.00, 0.00,  30.00, 16.00, 40.00, '#4A3728'),
    -- Sun room (room 31, 84x84)
    (54, 'Plant Stand',           'Tiered plant stand',                  31, NULL, 'Shelf',     4.00,   4.00,  0.00, 0.00,  36.00, 14.00, 30.00, '#6B4423'),
    (55, 'Storage Ottoman',       'Ottoman with storage',                31, NULL, 'Chest',    24.00,  40.00,  0.00, 0.00,  30.00, 18.00, 18.00, '#9CAF88'),
    -- Patio deck (room 32, 90x180)
    (56, 'Deck Box',              'Weatherproof deck box',               32, NULL, 'Chest',     6.00, 156.00,  0.00, 0.00,  50.00, 22.00, 24.00, '#5C5C5C'),
    (57, 'BBQ Grill',             'Gas grill with side cabinet',         32, NULL, 'Other',     6.00,  20.00,  0.00, 0.00,  30.00, 24.00, 46.00, '#333333'),
    -- Art studio (room 4, 96x174) - desk is container 3 above
    (58, 'Flat File Cabinet',     'Flat file for paper and prints',       4, NULL, 'Drawer',    8.00,   8.00,  0.00, 0.00,  40.00, 28.00, 30.00, '#8B8B8B'),
    (59, 'Canvas Rack',           'Vertical canvas storage rack',         4, NULL, 'Shelf',    62.00,   8.00,  0.00, 0.00,  30.00, 24.00, 60.00, '#6B4423'),
    -- Tool shop (room 33, 72x84)
    (60, 'Workbench',             'Workbench with lower shelf',          33, NULL, 'Other',    48.00,   8.00,  0.00, 0.00,  24.00, 60.00, 36.00, '#8B5A2B'),
    (61, 'Tool Chest',            'Rolling tool chest',                  33, NULL, 'Chest',     8.00,  60.00,  0.00, 0.00,  26.00, 18.00, 40.00, '#B22222'),
    -- Garage (room 5, 150x108) - cabinet is container 4 above
    (62, 'Garage Shelving',       'Heavy-duty shelving rack',             5, NULL, 'Shelf',   132.00,  26.00,  0.00, 0.00,  18.00, 48.00, 72.00, '#708090'),
    (63, 'Chest Freezer',         'Chest freezer',                        5, NULL, 'Chest',     4.00,  34.00,  0.00, 0.00,  36.00, 24.00, 34.00, '#F5F5F5'),
    -- Attic (room 8, 240x180)
    (64, 'Attic Totes',           'Stack of plastic storage totes',       8, NULL, 'Bin',      12.00,  12.00,  0.00, 0.00,  48.00, 36.00, 24.00, '#4682B4'),
    (65, 'Steamer Trunk',         'Old steamer trunk',                    8, NULL, 'Chest',    80.00,  20.00,  0.00, 0.00,  40.00, 22.00, 20.00, '#5C4033'),
    -- Car (room 11, 72x180): glove box in the dashboard up front, cargo trunk at the rear
    (66, 'Glove Box',             'Glove box in the dashboard',          11, NULL, 'Box',      48.00,   6.00, 24.00, 0.00,  16.00, 10.00,  6.00, '#2B2B2B'),
    (67, 'Trunk',                 'Rear cargo area',                     11, NULL, 'Chest',     6.00, 150.00,  0.00, 0.00,  60.00, 24.00, 30.00, '#3A3A3A'),
    -- Storage Unit: 20 moving boxes packed into the Storage room (room 6, 96x96), a 4x5 grid on the floor
    (68, 'Box 01', 'Storage box 1', 6, NULL, 'Box', 4.00, 2.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#C8A165'),
    (69, 'Box 02', 'Storage box 2', 6, NULL, 'Box', 27.00, 2.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#B8935A'),
    (70, 'Box 03', 'Storage box 3', 6, NULL, 'Box', 50.00, 2.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#CDA875'),
    (71, 'Box 04', 'Storage box 4', 6, NULL, 'Box', 73.00, 2.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#BF9E70'),
    (72, 'Box 05', 'Storage box 5', 6, NULL, 'Box', 4.00, 20.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#C8A165'),
    (73, 'Box 06', 'Storage box 6', 6, NULL, 'Box', 27.00, 20.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#B8935A'),
    (74, 'Box 07', 'Storage box 7', 6, NULL, 'Box', 50.00, 20.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#CDA875'),
    (75, 'Box 08', 'Storage box 8', 6, NULL, 'Box', 73.00, 20.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#BF9E70'),
    (76, 'Box 09', 'Storage box 9', 6, NULL, 'Box', 4.00, 38.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#C8A165'),
    (77, 'Box 10', 'Storage box 10', 6, NULL, 'Box', 27.00, 38.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#B8935A'),
    (78, 'Box 11', 'Storage box 11', 6, NULL, 'Box', 50.00, 38.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#CDA875'),
    (79, 'Box 12', 'Storage box 12', 6, NULL, 'Box', 73.00, 38.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#BF9E70'),
    (80, 'Box 13', 'Storage box 13', 6, NULL, 'Box', 4.00, 56.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#C8A165'),
    (81, 'Box 14', 'Storage box 14', 6, NULL, 'Box', 27.00, 56.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#B8935A'),
    (82, 'Box 15', 'Storage box 15', 6, NULL, 'Box', 50.00, 56.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#CDA875'),
    (83, 'Box 16', 'Storage box 16', 6, NULL, 'Box', 73.00, 56.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#BF9E70'),
    (84, 'Box 17', 'Storage box 17', 6, NULL, 'Box', 4.00, 74.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#C8A165'),
    (85, 'Box 18', 'Storage box 18', 6, NULL, 'Box', 27.00, 74.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#B8935A'),
    (86, 'Box 19', 'Storage box 19', 6, NULL, 'Box', 50.00, 74.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#CDA875'),
    (87, 'Box 20', 'Storage box 20', 6, NULL, 'Box', 73.00, 74.00, 0.00, 0.00, 18.00, 18.00, 18.00, '#BF9E70'),
    -- Grandmas townhouse furniture (rooms 3, 37-55). Positions are in room space, inches.
    (88, 'Furnace', 'Gas furnace', 38, NULL, 'Other', 8.00, 8.00, 0.00, 0.00, 30.00, 30.00, 60.00, '#8A8A8A'),
    (89, 'Water Heater', '40-gallon water heater', 38, NULL, 'Other', 44.00, 8.00, 0.00, 0.00, 24.00, 24.00, 60.00, '#D0D0D0'),
    (90, 'Workbench', 'Workbench with vise', 38, NULL, 'Other', 8.00, 88.00, 0.00, 0.00, 60.00, 24.00, 36.00, '#8B5A2B'),
    (91, 'Utility Shelving', 'Metal utility shelving', 38, NULL, 'Shelf', 120.00, 8.00, 0.00, 0.00, 30.00, 16.00, 72.00, '#708090'),
    (92, 'Basement Sofa', 'Sectional sofa', 39, NULL, 'Other', 8.00, 90.00, 0.00, 0.00, 84.00, 34.00, 32.00, '#6E7B8B'),
    (93, 'Media Cabinet', 'TV media cabinet', 39, NULL, 'Cabinet', 8.00, 8.00, 0.00, 0.00, 54.00, 18.00, 24.00, '#4A3728'),
    (94, 'Bookcase', 'Tall bookcase', 39, NULL, 'Shelf', 120.00, 8.00, 0.00, 0.00, 30.00, 12.00, 72.00, '#6B4423'),
    (95, 'Storage Shelving', 'Heavy-duty shelving', 40, NULL, 'Shelf', 8.00, 8.00, 0.00, 0.00, 48.00, 18.00, 72.00, '#708090'),
    (96, 'Storage Totes', 'Stacked storage totes', 40, NULL, 'Bin', 8.00, 80.00, 0.00, 0.00, 48.00, 24.00, 24.00, '#4682B4'),
    (97, 'Cedar Chest', 'Old cedar chest', 40, NULL, 'Chest', 110.00, 80.00, 0.00, 0.00, 40.00, 20.00, 20.00, '#5C4033'),
    (98, 'Coat Closet', 'Front hall coat closet', 41, NULL, 'Wardrobe', 2.00, 8.00, 0.00, 0.00, 22.00, 44.00, 84.00, '#D2B48C'),
    (99, 'Console Table', 'Entry console table', 41, NULL, 'Cabinet', 4.00, 300.00, 0.00, 0.00, 40.00, 14.00, 30.00, '#4A3728'),
    (100, 'Sofa', 'Three-seat sofa', 42, NULL, 'Other', 8.00, 90.00, 0.00, 0.00, 84.00, 36.00, 34.00, '#9CAF88'),
    (101, 'Armchair', 'Wingback armchair', 42, NULL, 'Other', 120.00, 90.00, 0.00, 0.00, 34.00, 34.00, 34.00, '#9CAF88'),
    (102, 'TV Console', 'TV console', 42, NULL, 'Cabinet', 8.00, 4.00, 0.00, 0.00, 60.00, 18.00, 24.00, '#4A3728'),
    (103, 'Bookcase', 'Living room bookcase', 42, NULL, 'Shelf', 130.00, 4.00, 0.00, 0.00, 24.00, 12.00, 72.00, '#6B4423'),
    (104, 'Kitchen Cabinets', 'Base cabinets with counter', 3, NULL, 'Cabinet', 0.00, 96.00, 0.00, 0.00, 144.00, 24.00, 36.00, '#C8A165'),
    (105, 'Upper Cabinets', 'Wall cabinets', 3, NULL, 'Cabinet', 0.00, 108.00, 54.00, 0.00, 144.00, 12.00, 30.00, '#C8A165'),
    (106, 'Refrigerator', 'Refrigerator', 3, NULL, 'Other', 120.00, 4.00, 0.00, 0.00, 24.00, 30.00, 68.00, '#DCDCDC'),
    (107, 'Pantry', 'Corner pantry', 3, NULL, 'Cabinet', 4.00, 4.00, 0.00, 0.00, 24.00, 24.00, 84.00, '#C8A165'),
    (108, 'Dining Table', 'Six-seat dining table', 43, NULL, 'Other', 20.00, 30.00, 0.00, 0.00, 54.00, 42.00, 30.00, '#4A3728'),
    (109, 'China Cabinet', 'China hutch', 43, NULL, 'Cabinet', 4.00, 4.00, 0.00, 0.00, 40.00, 18.00, 72.00, '#4A3728'),
    (110, 'Powder Vanity', 'Pedestal vanity', 44, NULL, 'Cabinet', 4.00, 4.00, 0.00, 0.00, 24.00, 18.00, 32.00, '#FFFFFF'),
    (111, 'Queen Bed', 'Queen bed', 46, NULL, 'Other', 40.00, 8.00, 0.00, 0.00, 60.00, 80.00, 24.00, '#7A5C44'),
    (112, 'Dresser', 'Six-drawer dresser', 46, NULL, 'Cabinet', 4.00, 8.00, 0.00, 0.00, 20.00, 54.00, 34.00, '#7A5C44'),
    (113, 'Nightstand (L)', 'Nightstand', 46, NULL, 'Cabinet', 40.00, 96.00, 0.00, 0.00, 18.00, 16.00, 24.00, '#7A5C44'),
    (114, 'Wardrobe', 'Primary wardrobe', 46, NULL, 'Wardrobe', 120.00, 8.00, 0.00, 0.00, 30.00, 26.00, 84.00, '#D2B48C'),
    (115, 'Double Vanity', 'Double-sink vanity', 47, NULL, 'Cabinet', 4.00, 4.00, 0.00, 0.00, 48.00, 21.00, 32.00, '#FFFFFF'),
    (116, 'Linen Cabinet', 'Bathroom linen cabinet', 47, NULL, 'Cabinet', 120.00, 4.00, 0.00, 0.00, 24.00, 16.00, 66.00, '#FFFFFF'),
    (117, 'Full Bed', 'Full bed', 48, NULL, 'Other', 40.00, 8.00, 0.00, 0.00, 54.00, 75.00, 22.00, '#8B7355'),
    (118, 'Dresser', 'Dresser', 48, NULL, 'Cabinet', 4.00, 8.00, 0.00, 0.00, 20.00, 48.00, 34.00, '#8B5A2B'),
    (119, 'Closet', 'Bedroom closet', 48, NULL, 'Wardrobe', 120.00, 8.00, 0.00, 0.00, 30.00, 24.00, 84.00, '#D2B48C'),
    (120, 'Twin Bed', 'Twin bed', 50, NULL, 'Other', 40.00, 8.00, 0.00, 0.00, 38.00, 75.00, 22.00, '#8B7355'),
    (121, 'Dresser', 'Dresser', 50, NULL, 'Cabinet', 4.00, 8.00, 0.00, 0.00, 20.00, 44.00, 34.00, '#8B5A2B'),
    (122, 'Closet', 'Bedroom closet', 50, NULL, 'Wardrobe', 120.00, 8.00, 0.00, 0.00, 30.00, 24.00, 84.00, '#D2B48C'),
    (123, 'Desk', 'Writing desk', 51, NULL, 'Cabinet', 8.00, 8.00, 0.00, 0.00, 60.00, 30.00, 30.00, '#6B4423'),
    (124, 'Bookshelf', 'Study bookshelf', 51, NULL, 'Shelf', 120.00, 8.00, 0.00, 0.00, 30.00, 12.00, 72.00, '#6B4423'),
    (125, 'File Cabinet', 'Two-drawer file cabinet', 51, NULL, 'Drawer', 8.00, 80.00, 0.00, 0.00, 18.00, 24.00, 30.00, '#8B8B8B'),
    (126, 'Vanity', 'Sink vanity', 52, NULL, 'Cabinet', 4.00, 4.00, 0.00, 0.00, 30.00, 21.00, 32.00, '#FFFFFF'),
    (127, 'Linen Shelves', 'Linen shelving', 53, NULL, 'Shelf', 4.00, 4.00, 0.00, 0.00, 30.00, 16.00, 72.00, '#D2B48C'),
    (128, 'Attic Shelving', 'Attic shelving', 55, NULL, 'Shelf', 8.00, 8.00, 0.00, 0.00, 48.00, 18.00, 60.00, '#708090'),
    (129, 'Attic Totes', 'Stacked totes', 55, NULL, 'Bin', 8.00, 100.00, 0.00, 0.00, 48.00, 36.00, 24.00, '#4682B4'),
    (130, 'Steamer Trunk', 'Antique steamer trunk', 55, NULL, 'Chest', 100.00, 20.00, 0.00, 0.00, 40.00, 22.00, 20.00, '#5C4033')
) AS source (Id, Name, Description, RoomId, ParentContainerId, ContainerType, PositionXInches, PositionYInches, PositionZInches, Rotation, WidthInches, DepthInches, HeightInches, Color)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Description = source.Description,
    RoomId = source.RoomId,
    ParentContainerId = source.ParentContainerId,
    ContainerType = source.ContainerType,
    PositionXInches = source.PositionXInches,
    PositionYInches = source.PositionYInches,
    PositionZInches = source.PositionZInches,
    Rotation = source.Rotation,
    WidthInches = source.WidthInches,
    DepthInches = source.DepthInches,
    HeightInches = source.HeightInches,
    Color = source.Color
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description, RoomId, ParentContainerId, ContainerType, PositionXInches, PositionYInches, PositionZInches, Rotation, WidthInches, DepthInches, HeightInches, Color)
    VALUES (source.Id, source.Name, source.Description, source.RoomId, source.ParentContainerId, source.ContainerType, source.PositionXInches, source.PositionYInches, source.PositionZInches, source.Rotation, source.WidthInches, source.DepthInches, source.HeightInches, source.Color);

SET IDENTITY_INSERT dbo.Container OFF;
