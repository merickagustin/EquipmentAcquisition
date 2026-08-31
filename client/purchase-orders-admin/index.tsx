import React from 'react';
import { createRoot } from 'react-dom/client';
import { ThemeProvider } from '@mui/material';
import { theme } from '../shared/theme';
import { PurchaseOrdersAdminApp } from './PurchaseOrdersAdminApp';

const container = document.getElementById('content-root');
if (container) {
  createRoot(container).render(
    <ThemeProvider theme={theme}>
      <PurchaseOrdersAdminApp />
    </ThemeProvider>,
  );
}
