import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import bgImage from "../assets/photo3.jpg";

const Layout = ({ children }) => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const getDashboardLink = () => {
    if (!user) return '/';
    if (user.roles.includes('Manager')) return '/manager';
    if (user.roles.includes('Mechanic')) return '/mechanic';
    if (user.roles.includes('Client')) return '/client';
    return '/';
  };

  const isClient = user?.roles.includes('Client');

  return (
    <div 
      className={`min-h-screen w-full ${isClient ? 'bg-cover bg-center bg-fixed' : 'bg-gray-50'}`}
      style={isClient ? { backgroundImage: `url(${bgImage})` } : {}}
    >
      <nav className="bg-white shadow-md">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex">
              <Link to={getDashboardLink()} className="flex items-center">
                <span className="text-2xl font-bold text-primary-600">Vehicle Service</span>
              </Link>
              <div className="hidden sm:ml-6 sm:flex sm:space-x-8">
                {user?.roles.includes('Manager') && (
                  <>
                    <Link to="/manager" className="text-gray-700 hover:text-primary-600 px-3 py-2 text-sm font-medium">
                      Dashboard
                    </Link>
                  </>
                )}
                {user?.roles.includes('Mechanic') && (
                  <>
                    <Link to="/mechanic" className="text-gray-700 hover:text-primary-600 px-3 py-2 text-sm font-medium">
                      Dashboard
                    </Link>
                  </>
                )}
                {user?.roles.includes('Client') && (
                  <>
                    <Link to="/client" className="text-gray-700 hover:text-primary-600 px-3 py-2 text-sm font-medium">
                     
                    </Link>
                  </>
                )}
              </div>
            </div>
            <div className="flex items-center">
              {user && (
                <div className="flex items-center space-x-4">
                  <span className="text-gray-700 text-sm">
                    {user.firstName} {user.lastName} ({user.roles[0]})
                  </span>
                  <button
                    onClick={handleLogout}
                    className="bg-accent-500 hover:bg-accent-600 text-white px-4 py-2 rounded-md text-sm font-medium"
                  >
                    Logout
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </nav>
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        {children}
      </main>
    </div>
  );
};

export default Layout;