import React from 'react';
import { createRoot } from 'react-dom/client';
import { ThemeProvider } from '@mui/material';
import { theme } from '../shared/theme';
import { HomeApp } from './HomeApp';

const container = document.getElementById('content-root');
if (container) {
  createRoot(container).render(
    <ThemeProvider theme={theme}>
      <HomeApp />
    </ThemeProvider>,
  );
}
