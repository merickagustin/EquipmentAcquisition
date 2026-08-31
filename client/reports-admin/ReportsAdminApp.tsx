import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  CircularProgress,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableFooter,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { apiClient } from '../shared/apiClient';
import type { DepartmentDto, ReportRowDto } from '../shared/types';

function isoDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}

function defaultFrom(): string {
  const d = new Date();
  d.setFullYear(d.getFullYear() - 3);
  return isoDate(d);
}

export function ReportsAdminApp() {
  const [departments, setDepartments] = useState<DepartmentDto[] | null>(null);
  const [refDataError, setRefDataError] = useState<string | null>(null);

  const [departmentId, setDepartmentId] = useState<number | ''>('');
  const [from, setFrom] = useState(defaultFrom());
  const [to, setTo] = useState(isoDate(new Date()));

  const [rows, setRows] = useState<ReportRowDto[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    apiClient
      .get<DepartmentDto[]>('/api/departments')
      .then(setDepartments)
      .catch((e: Error) => setRefDataError(e.message));
  }, []);

  const loadReport = () => {
    setLoading(true);
    const params = new URLSearchParams({
      from,
      // End-of-day, not the bare date — see requests-admin's identical fix for why
      // a bare date binds to midnight and would exclude same-day spend.
      to: `${to}T23:59:59.999`,
    });
    if (departmentId !== '') params.set('departmentId', String(departmentId));
    apiClient
      .get<ReportRowDto[]>(`/api/reports/department-spend?${params.toString()}`)
      .then((data) => {
        setRows([...data].sort((a, b) => b.totalSpend - a.totalSpend));
        setLoadError(null);
      })
      .catch((e: Error) => setLoadError(e.message))
      .finally(() => setLoading(false));
  };

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(loadReport, [departmentId, from, to]);

  if (refDataError) return <Alert severity="error">{refDataError}</Alert>;
  if (!departments) return <CircularProgress />;

  const totalRequests = rows?.reduce((sum, r) => sum + r.requestCount, 0) ?? 0;
  const totalSpend = rows?.reduce((sum, r) => sum + r.totalSpend, 0) ?? 0;

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Department Spend Report
      </Typography>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
          <TextField
            select
            label="Department"
            size="small"
            sx={{ minWidth: 180 }}
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value === '' ? '' : Number(e.target.value))}
          >
            <MenuItem value="">All Departments</MenuItem>
            {departments.map((d) => (
              <MenuItem key={d.id} value={d.id}>
                {d.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            type="date"
            label="From"
            size="small"
            InputLabelProps={{ shrink: true }}
            value={from}
            onChange={(e) => setFrom(e.target.value)}
          />
          <TextField
            type="date"
            label="To"
            size="small"
            InputLabelProps={{ shrink: true }}
            value={to}
            onChange={(e) => setTo(e.target.value)}
          />
        </Stack>
      </Paper>

      {loadError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {loadError}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Department</TableCell>
              <TableCell>Category</TableCell>
              <TableCell align="right">Requests</TableCell>
              <TableCell align="right">Total Spend</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={4} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            )}
            {!loading && rows?.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} align="center">
                  No spend in this range.
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              rows?.map((r, i) => (
                <TableRow key={`${r.departmentName}-${r.categoryName}-${i}`}>
                  <TableCell>{r.departmentName}</TableCell>
                  <TableCell>{r.categoryName}</TableCell>
                  <TableCell align="right">{r.requestCount}</TableCell>
                  <TableCell align="right">${r.totalSpend.toFixed(2)}</TableCell>
                </TableRow>
              ))}
          </TableBody>
          {!loading && rows && rows.length > 0 && (
            <TableFooter>
              <TableRow>
                <TableCell colSpan={2}>
                  <strong>Total</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>{totalRequests}</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>${totalSpend.toFixed(2)}</strong>
                </TableCell>
              </TableRow>
            </TableFooter>
          )}
        </Table>
      </TableContainer>
    </Box>
  );
}
