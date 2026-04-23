import { Routes, Route, Navigate } from 'react-router-dom'
import { AuthGuard } from './components/layout/AuthGuard'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { ForgotPasswordPage } from './pages/ForgotPasswordPage'
import { ResetPasswordPage } from './pages/ResetPasswordPage'
import { CreateGardenPage } from './pages/CreateGardenPage'
import { EditGardenPage } from './pages/EditGardenPage'
import { GardenDetailPage } from './pages/GardenDetailPage'
import { CreateGardenBedPage } from './pages/CreateGardenBedPage'
import { EditGardenBedPage } from './pages/EditGardenBedPage'
import { GardenBedDetailPage } from './pages/GardenBedDetailPage'
import { CreatePlantingPage } from './pages/CreatePlantingPage'
import { EditPlantingPage } from './pages/EditPlantingPage'
import { PlantingDetailPage } from './pages/PlantingDetailPage'
import { CreateGardenTaskPage } from './pages/CreateGardenTaskPage'
import { EditGardenTaskPage } from './pages/EditGardenTaskPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route
        path="/"
        element={<AuthGuard><DashboardPage /></AuthGuard>}
      />
      <Route
        path="/gardens/new"
        element={<AuthGuard><CreateGardenPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id"
        element={<AuthGuard><GardenDetailPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id/edit"
        element={<AuthGuard><EditGardenPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id/tasks/new"
        element={<AuthGuard><CreateGardenTaskPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id/tasks/:taskId/edit"
        element={<AuthGuard><EditGardenTaskPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id/beds/new"
        element={<AuthGuard><CreateGardenBedPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id/beds/:bedId"
        element={<AuthGuard><GardenBedDetailPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id/beds/:bedId/edit"
        element={<AuthGuard><EditGardenBedPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id/beds/:bedId/plantings/new"
        element={<AuthGuard><CreatePlantingPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id/plantings/:plantingId"
        element={<AuthGuard><PlantingDetailPage /></AuthGuard>}
      />
      <Route
        path="/gardens/:id/plantings/:plantingId/edit"
        element={<AuthGuard><EditPlantingPage /></AuthGuard>}
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
