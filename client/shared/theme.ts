import { createTheme } from '@mui/material/styles';

// Standard MUI palette — blue primary, light background, default Roboto
// typography. No custom branding; this is a portfolio piece demonstrating
// the architecture, not a themed product. See docs/architecture.md.
export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#1976d2' },
    background: { default: '#f5f5f5' },
  },
  typography: {
    fontFamily: 'Roboto, "Helvetica Neue", Arial, sans-serif',
  },
});
