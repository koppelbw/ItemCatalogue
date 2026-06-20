import { Routes, Route } from 'react-router'
import LocationsPage from './pages/LocationsPage'


function App() {

  return (
  <Routes>
    <Route path='/' element={<LocationsPage />} />
  </Routes>
  )
}

export default App
