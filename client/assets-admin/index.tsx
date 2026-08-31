import React from 'react';
import { createRoot } from 'react-dom/client';
import { ThemeProvider } from '@mui/material';
import { theme } from '../shared/theme';
import { AssetsAdminApp } from './AssetsAdminApp';

const container = document.getElementById('content-root');
if (container) {
  createRoot(container).render(
    <ThemeProvider theme={theme}>
      <AssetsAdminApp />
    </ThemeProvider>,
  );
}
