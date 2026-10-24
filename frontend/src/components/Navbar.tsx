import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Shield, LogOut, Upload, Users, FileText, BarChart2 } from 'lucide-react';

export function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <nav className="bg-slate-900 text-white shadow-lg">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between h-16 items-center">
          <Link to="/documents" className="flex items-center gap-2 font-bold text-xl hover:text-slate-300">
            <Shield className="w-6 h-6 text-blue-400" />
            TrustVault
          </Link>

          <div className="flex items-center gap-4">
            <Link to="/documents" className="flex items-center gap-1 text-sm hover:text-blue-300 transition-colors">
              <FileText className="w-4 h-4" />
              Documents
            </Link>

            {(user?.role === 'Owner' || user?.role === 'Administrator') && (
              <Link to="/documents/upload" className="flex items-center gap-1 text-sm hover:text-blue-300 transition-colors">
                <Upload className="w-4 h-4" />
                Upload
              </Link>
            )}

            {user?.role === 'Administrator' && (
              <>
                <Link to="/admin/users" className="flex items-center gap-1 text-sm hover:text-blue-300 transition-colors">
                  <Users className="w-4 h-4" />
                  Users
                </Link>
                <Link to="/admin/audit" className="flex items-center gap-1 text-sm hover:text-blue-300 transition-colors">
                  <BarChart2 className="w-4 h-4" />
                  Audit
                </Link>
              </>
            )}

            <div className="flex items-center gap-3 ml-4 border-l border-slate-700 pl-4">
              <span className="text-sm text-slate-300">{user?.fullName}</span>
              <span className="text-xs px-2 py-0.5 rounded-full bg-blue-600 text-white">{user?.role}</span>
              <button
                onClick={handleLogout}
                className="flex items-center gap-1 text-sm text-slate-300 hover:text-red-400 transition-colors"
              >
                <LogOut className="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>
      </div>
    </nav>
  );
}
