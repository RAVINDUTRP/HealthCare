import React, { useState } from 'react';
import { ArrowLeft, Eye, EyeOff, User, Lock, Mail } from 'lucide-react';
import { login, loginDoctor, loginPharmacist, loginPatient } from '../api/api';

const LoginForm = ({ onBack, onSuccess }) => {
  const [showPassword, setShowPassword] = useState(false);
  const [formData, setFormData] = useState({
    usernameOrEmail: '',
    password: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState({});

  const handleChange = (name, value) => {
    setFormData(prev => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: '' }));
    }
  };

  const validateForm = () => {
    const newErrors = {};
    
    if (!formData.usernameOrEmail.trim()) {
      newErrors.usernameOrEmail = 'Username or email is required';
    }
    
    if (!formData.password) {
      newErrors.password = 'Password is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!validateForm()) return;

    setIsLoading(true);
    try {
      // Determine which login API to use based on the user's role
      // Since we don't know the role yet, we'll try the general login first
      const response = await login({
        usernameOrEmail: formData.usernameOrEmail,
        password: formData.password
      });
      
      const data = response.data;
      const token = data.token || data.Token;
      const username = data.username || data.Username;
      const role = data.role || data.Role;
      const avatarColor = data.avatarColor;
      const avatarEmoji = data.avatarEmoji;
      const profileImageUrl = data.profileImageUrl;
      
      if (!token) throw new Error('No token returned from server');
      
      // Store complete user data including avatar
      const userData = {
        username,
        role,
        avatarColor,
        avatarEmoji,
        profileImageUrl: profileImageUrl || 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?auto=format&fit=crop&w=150&q=80',
        email: formData.usernameOrEmail.includes('@') ? formData.usernameOrEmail : ''
      };
      
      onSuccess({
        usernameOrEmail: formData.usernameOrEmail,
        password: formData.password,
        response: data
      });
    } catch (error) {
      console.error('Login error:', error);
      setErrors({ submit: error.response?.data || 'Login failed. Please check your credentials.' });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50 flex items-center justify-center p-4">
      <div className="max-w-md w-full">
        {/* Header */}
        <div className="text-center mb-8">
          <button
            onClick={onBack}
            className="inline-flex items-center gap-2 text-emerald-600 hover:text-emerald-700 mb-6 transition-colors"
          >
            <ArrowLeft className="w-5 h-5" />
            Back to Role Selection
          </button>
          
          <div className="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-br from-emerald-500 to-teal-500 rounded-2xl text-white shadow-lg mb-4">
            <User className="w-8 h-8" />
          </div>
          
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Welcome Back</h1>
          <p className="text-gray-600">Sign in to your HealthCare+ account</p>
        </div>

        {/* Form */}
        <div className="bg-white rounded-3xl shadow-xl p-8">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                Username or Email <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <div className="absolute left-4 top-1/2 transform -translate-y-1/2 text-gray-500">
                  <User className="w-5 h-5" />
                </div>
                <input
                  type="text"
                  value={formData.usernameOrEmail}
                  onChange={(e) => handleChange('usernameOrEmail', e.target.value)}
                  className={`w-full px-4 py-3 pl-12 border rounded-xl focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-colors ${
                    errors.usernameOrEmail ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="Enter your username or email"
                  required
                />
              </div>
              {errors.usernameOrEmail && (
                <p className="mt-1 text-sm text-red-600">{errors.usernameOrEmail}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                Password <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <div className="absolute left-4 top-1/2 transform -translate-y-1/2 text-gray-500">
                  <Lock className="w-5 h-5" />
                </div>
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={formData.password}
                  onChange={(e) => handleChange('password', e.target.value)}
                  className={`w-full px-4 py-3 pl-12 pr-12 border rounded-xl focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-colors ${
                    errors.password ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="Enter your password"
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-4 top-1/2 transform -translate-y-1/2 text-gray-500 hover:text-gray-700"
                >
                  {showPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-sm text-red-600">{errors.password}</p>
              )}
            </div>

            <div className="flex items-center justify-between">
              <label className="flex items-center">
                <input
                  type="checkbox"
                  className="rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
                />
                <span className="ml-2 text-sm text-gray-600">Remember me</span>
              </label>
              <button
                type="button"
                className="text-sm text-emerald-600 hover:text-emerald-700 transition-colors"
              >
                Forgot password?
              </button>
            </div>

            {errors.submit && (
              <div className="p-4 bg-red-50 border border-red-200 rounded-xl">
                <p className="text-red-600 text-sm">{errors.submit}</p>
              </div>
            )}

            <button
              type="submit"
              disabled={isLoading}
              className="w-full py-4 bg-gradient-to-r from-emerald-500 to-teal-500 text-white rounded-xl font-bold text-lg shadow-lg hover:shadow-xl transform hover:-translate-y-1 transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none"
            >
              {isLoading ? 'Signing In...' : 'Sign In'}
            </button>
          </form>

          <div className="mt-6 text-center">
            <p className="text-gray-600">
              Don't have an account?{' '}
              <button
                onClick={onBack}
                className="text-emerald-600 font-semibold hover:text-emerald-700 transition-colors"
              >
                Register here
              </button>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginForm;