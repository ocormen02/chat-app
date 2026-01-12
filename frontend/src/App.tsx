import React, { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import theme from './theme/theme';
import Login from './components/Login';
import Register from './components/Register';
import ChatroomsPage from './components/ChatroomsPage';
import authService from './services/authService';

const PrivateRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(authService.isAuthenticated());

  useEffect(() => {
    // Check authentication status on mount
    const checkAuth = () => {
      const authenticated = authService.isAuthenticated();
      setIsAuthenticated(authenticated);
    };

    // Check immediately on mount
    checkAuth();

    // Listen for storage events (when localStorage is changed from other tabs/windows)
    // This allows synchronization across browser tabs
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'authToken' || e.key === null) {
        checkAuth();
      }
    };

    window.addEventListener('storage', handleStorageChange);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
    };
  }, []);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
};

const App: React.FC = () => {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route
            path="/chatrooms"
            element={
              <PrivateRoute>
                <ChatroomsPage />
              </PrivateRoute>
            }
          />
          <Route path="/" element={<Navigate to="/chatrooms" />} />
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  );
};

export default App;
