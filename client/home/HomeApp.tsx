import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { apiClient } from '../shared/apiClient';
import type { DepartmentPendingCountDto, MenuItemDto } from '../shared/types';

export function HomeApp() {
  const [menuItems, setMenuItems] = useState<MenuItemDto[] | null>(null);
  const [pending, setPending] = useState<DepartmentPendingCountDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient
      .get<MenuItemDto[]>('/api/menu-items')
      .then(setMenuItems)
      .catch((e: Error) => setError(e.message));
  }, []);

  // The widget only shows if the Requests menu entry itself is active — the same
  // flag that controls whether /requests is reachable from the sidebar at all, so
  // toggling it in Menu Admin has a visible effect here too, not just in the nav.
  const requestsMenuActive = menuItems?.some((m) => m.route === '/requests' && m.isActive) ?? false;

  useEffect(() => {
    if (!requestsMenuActive) return;
    apiClient
      .get<DepartmentPendingCountDto[]>('/api/acquisition-requests/pending-by-department')
      .then(setPending)
      .catch((e: Error) => setError(e.message));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [requestsMenuActive]);

  if (error) return <Alert severity="error">{error}</Alert>;
  if (!menuItems) return <CircularProgress />;

  const totalPending = pending?.reduce((sum, d) => sum + d.pendingCount, 0) ?? 0;

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 1 }}>
        Equipment Acquisition
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Requests, approvals, purchase orders, and asset assignment across departments.
      </Typography>

      {!requestsMenuActive && (
        <Alert severity="info">
          Pending requisition counts appear here once the Acquisitions → Requests menu entry is
          active. Toggle it on in Menu Admin to see this widget.
        </Alert>
      )}

      {requestsMenuActive && !pending && <CircularProgress size={24} />}

      {requestsMenuActive && pending && (
        <Box>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Pending Requisitions by Department
            <Chip label={`${totalPending} total`} size="small" sx={{ ml: 1 }} />
          </Typography>
          <TableContainer component={Paper}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Department</TableCell>
                  <TableCell align="right">Pending</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {pending.map((row) => (
                  <TableRow key={row.departmentId}>
                    <TableCell>{row.departmentName}</TableCell>
                    <TableCell align="right">
                      {row.pendingCount > 0 ? (
                        <Chip label={row.pendingCount} size="small" color="warning" />
                      ) : (
                        <span>0</span>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      )}
    </Box>
  );
}
