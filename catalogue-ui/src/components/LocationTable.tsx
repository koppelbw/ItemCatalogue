import type { LocationResponse } from "../api/types";

interface LocationTableProps {
    locations: LocationResponse[];
    onEdit: (location: LocationResponse) => void;
    onDelete: (id: number) => void;
}

function LocationTable({locations, onEdit, onDelete}: LocationTableProps){
    return (
        <table>
          <thead>
            <tr>
              <th>Id</th>
              <th>Name</th>
              <th>Description</th>
              <th>Rooms</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {locations.map((loc) => (
              <tr key={loc.id}>
                <td>{loc.id}</td>
                <td>{loc.name}</td>
                <td>{loc.description}</td>
                <td>{loc.rooms.length}</td>
                <td>
                  <button onClick={() => onEdit(loc)}>Edit</button>{' '}
                  <button onClick={() => onDelete(loc.id)}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
    );
}

export default LocationTable