-- The official Microsoft Learn guide explains that the build engine automatically recognizes this file and includes it in the .dacpac.
-- This script is intended to be run after the deployment of the database. It can be used to insert seed data, create indexes, 
-- or perform any necessary post-deployment tasks.


--  Set the Build Action for these sub-scripts to None in the Properties pane to prevent them from being compiled as schema objects.


-- Location seeds before Room: Room.LocationId is a required FK to Location.
:r .\Seed_Location.sql
:r .\Seed_Room.sql
:r .\Seed_Person.sql
:r .\Seed_Item.sql