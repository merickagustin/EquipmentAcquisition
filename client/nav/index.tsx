import React from 'react';
import { createRoot } from 'react-dom/client';
import { ThemeProvider } from '@mui/material';
import { theme } from '../shared/theme';
import { NavApp } from './NavApp';

const container = document.getElementById('nav-root');
if (container) {
  createRoot(container).render(
    <ThemeProvider theme={theme}>
      <NavApp />
    </ThemeProvider>,
  );
}
