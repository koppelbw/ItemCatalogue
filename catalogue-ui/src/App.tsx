import { Routes, Route } from 'react-router'
import LocationsPage from './pages/LocationsPage'
import LocationDetailPage from './pages/LocationDetailPage'


function App() {

  return (
  <Routes>
    <Route path='/' element={<LocationsPage />} />
    <Route path='/locations/:id' element={<LocationDetailPage />}/>
  </Routes>
  )
}

export default App
