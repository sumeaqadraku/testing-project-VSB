import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import PrivateRoute from './components/PrivateRoute';
import Layout from './components/Layout';
import Login from './pages/Login';
import Register from './pages/Register';
import ManagerDashboard from './pages/ManagerDashboard';
import MechanicDashboard from './pages/MechanicDashboard';
import ClientDashboard from './pages/ClientDashboard';

function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route
            path="/manager"
            element={
              <PrivateRoute allowedRoles={['Manager']}>
                <ManagerDashboard />
              </PrivateRoute>
            }
          />
          <Route
            path="/mechanic"
            element={
              <PrivateRoute allowedRoles={['Mechanic']}>
                <MechanicDashboard />
              </PrivateRoute>
            }
          />
          <Route
            path="/client"
            element={
              <PrivateRoute allowedRoles={['Client']}>
                <ClientDashboard />
              </PrivateRoute>
            }
          />
          <Route path="/" element={<Navigate to="/login" replace />} />
        </Routes>
      </Router>
    </AuthProvider>
  );
}

export default App;

