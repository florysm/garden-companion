import { createTheme } from '@mui/material/styles'

const theme = createTheme({
  palette: {
    primary:    { main: '#6B7F5E' },   // sage green — ambient, nav, chips
    secondary:  { main: '#C4714A' },   // terracotta — primary CTAs
    background: { default: '#F7F4EE', paper: '#EEEBE4' },
    text:       { primary: '#2C2C28', secondary: '#7A786F' },
    error:      { main: '#B85C4A' },
  },
  typography: {
    fontFamily: '"Inter", sans-serif',
    h1: { fontFamily: '"Spectral", serif', fontWeight: 600 },
    h2: { fontFamily: '"Spectral", serif', fontWeight: 600 },
    h3: { fontFamily: '"Spectral", serif', fontWeight: 600 },
    h4: { fontFamily: '"Spectral", serif', fontWeight: 600 },
    h5: { fontFamily: '"Spectral", serif', fontWeight: 600 },
    h6: { fontFamily: '"Spectral", serif', fontWeight: 600 },
  },
  shape: { borderRadius: 12 },
  components: {
    MuiCssBaseline: {
      styleOverrides: { body: { backgroundColor: '#F7F4EE' } },
    },
    MuiCard: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: {
          backgroundColor: '#EEEBE4',
          borderRadius: 12,
          boxShadow: '0 2px 8px rgba(44,44,40,0.07)',
          border: 'none',
        },
      },
    },
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: { textTransform: 'none', fontWeight: 500, borderRadius: 8 },
      },
    },
    MuiChip: {
      styleOverrides: { root: { borderRadius: 6 } },
    },
    MuiTextField: {
      defaultProps: { variant: 'outlined', fullWidth: true },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          backgroundColor: '#F7F4EE',
          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: '#C8C5BE',
          },
          '&:hover .MuiOutlinedInput-notchedOutline': {
            borderColor: '#6B7F5E',
          },
        },
      },
    },
  },
})

export default theme
