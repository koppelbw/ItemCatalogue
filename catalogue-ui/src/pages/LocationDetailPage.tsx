import { useEffect, useState } from "react";
import { useParams, Link } from "react-router"
import { API_BASE } from '../api/client';

import type { LocationResponse } from '../api/types'

function LocationDetailPage(){
    const {id} = useParams();
    const [location, setLocation] = useState<LocationResponse | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [loading, setLoading] = useState<boolean | null>(true)



    async function loadLocation(){
        try {
            const res = await fetch(`${API_BASE}/api/locations/${id}`)
            
            if(res.status === 404){
                throw new Error("Location not found");
            }
            if (!res.ok) {
                throw new Error(`HTTP ${res.status}`)
            }
            
             const page = (await res.json()) as LocationResponse
            
            setLocation(page)
        } 
        catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to load')
        } 
        finally {
            setLoading(false)
        }
    }
    

    useEffect(() =>{
        loadLocation();
    }, [id]) 



    return (
        <main>
            <h1>Location Detail Page</h1>
            <Link to="/">Back to locations</Link>
            {
                loading ? 
                (
                    <p>Loading ...</p>
                )
                : error ?
                (
                    <p className='error'>{error}</p>
                )
                :
                (
                    <div>
                        <h2>{location?.name}</h2>
                        <i>{location?.description}</i>
                        <ul>
                            {location?.rooms.map(room => 
                                <li key={room.id}>{room.name}</li>
                            )}
                        </ul>
                    </div>
                )
            }
        </main>
    )
}

export default LocationDetailPage