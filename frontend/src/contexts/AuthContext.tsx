import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import { authApi } from '../api/auth'

interface AuthUser {
  userId: string
  householdId: string | null
  email: string
  displayName: string
}

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

function readStoredUser(): AuthUser | null {
  const token = localStorage.getItem('accessToken')
  const userId = localStorage.getItem('userId')
  const email = localStorage.getItem('email')
  const displayName = localStorage.getItem('displayName')
  if (token && userId && email && displayName) {
    return {
      userId,
      householdId: localStorage.getItem('householdId'),
      email,
      displayName,
    }
  }
  return null
}

function storeUser(data: { userId: string; householdId: string | null; email: string; displayName: string; accessToken: string; refreshToken: string }) {
  localStorage.setItem('accessToken', data.accessToken)
  localStorage.setItem('refreshToken', data.refreshToken)
  localStorage.setItem('userId', data.userId)
  localStorage.setItem('email', data.email)
  localStorage.setItem('displayName', data.displayName)
  if (data.householdId) {
    localStorage.setItem('householdId', data.householdId)
  } else {
    localStorage.removeItem('householdId')
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(readStoredUser)

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await authApi.login({ email, password })
    storeUser(data)
    setUser({ userId: data.userId, householdId: data.householdId, email: data.email, displayName: data.displayName })
  }, [])

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const { data } = await authApi.register({ email, password, displayName })
    storeUser(data)
    setUser({ userId: data.userId, householdId: data.householdId, email: data.email, displayName: data.displayName })
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('userId')
    localStorage.removeItem('email')
    localStorage.removeItem('displayName')
    localStorage.removeItem('householdId')
    setUser(null)
  }, [])

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: user !== null, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
