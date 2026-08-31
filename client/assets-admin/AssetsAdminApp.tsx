import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import { apiClient } from '../shared/apiClient';
import { ConfirmDialog } from '../shared/components/ConfirmDialog';
import { FormDialog } from '../shared/components/FormDialog';
import {
  AssetStatus,
  assetStatusLabel,
  type AssetDto,
  type AssetStatusValue,
  type CreateAssetDto,
  type DepartmentDto,
  type PagedResult,
  type PurchaseOrderDto,
  type UpdateAssetDto,
} from '../shared/types';

const emptyCreateForm: CreateAssetDto = {
  purchaseOrderId: 0,
  departmentId: 0,
  assetTag: '',
  serialNumber: '',
  status: AssetStatus.InStock,
};

const statusChipColor: Record<AssetStatusValue, 'default' | 'primary' | 'warning' | 'error'> = {
  0: 'default',
  1: 'primary',
  2: 'warning',
  3: 'error',
};

export function AssetsAdminApp() {
  const [departments, setDepartments] = useState<DepartmentDto[] | null>(null);
  const [refDataError, setRefDataError] = useState<string | null>(null);
  const departmentName = (id: number) => departments?.find((d) => d.id === id)?.name ?? `#${id}`;

  const [departmentId, setDepartmentId] = useState<number | ''>('');
  const [status, setStatus] = useState<AssetStatusValue | ''>('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const [grid, setGrid] = useState<PagedResult<AssetDto> | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [createForm, setCreateForm] = useState<CreateAssetDto>(emptyCreateForm);
  const [editForm, setEditForm] = useState<UpdateAssetDto>({ departmentId: 0, status: AssetStatus.InStock });
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [poLookup, setPoLookup] = useState<PurchaseOrderDto | null>(null);
  const [poLookupError, setPoLookupError] = useState<string | null>(null);
  const [poLookingUp, setPoLookingUp] = useState(false);

  const [deleteTarget, setDeleteTarget] = useState<AssetDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  useEffect(() => {
    apiClient.get<DepartmentDto[]>('/api/departments').catch((e: Error) => {
      setRefDataError(e.message);
      return null;
    }).then((d) => d && setDepartments(d));
  }, []);

  const loadGrid = () => {
    setLoading(true);
    const params = new URLSearchParams({ PageNumber: String(pageNumber), PageSize: String(pageSize) });
    if (departmentId !== '') params.set('DepartmentId', String(departmentId));
    if (status !== '') params.set('Status', String(status));
    apiClient
      .get<PagedResult<AssetDto>>(`/api/assets/grid?${params.toString()}`)
      .then((data) => {
        setGrid(data);
        setLoadError(null);
      })
      .catch((e: Error) => setLoadError(e.message))
      .finally(() => setLoading(false));
  };

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(loadGrid, [departmentId, status, pageNumber, pageSize]);

  const changeFilter = (apply: () => void) => {
    apply();
    setPageNumber(1);
  };

  const openCreate = () => {
    setEditingId(null);
    setCreateForm({ ...emptyCreateForm, departmentId: typeof departmentId === 'number' ? departmentId : 0 });
    setPoLookup(null);
    setPoLookupError(null);
    setFormError(null);
    setDialogOpen(true);
  };

  const openEdit = (asset: AssetDto) => {
    setEditingId(asset.id);
    setEditForm({ departmentId: asset.departmentId, status: asset.status });
    setFormError(null);
    setDialogOpen(true);
  };

  const lookupPo = async (purchaseOrderId: number) => {
    if (!purchaseOrderId) {
      setPoLookup(null);
      setPoLookupError(null);
      return;
    }
    setPoLookingUp(true);
    setPoLookup(null);
    setPoLookupError(null);
    try {
      const po = await apiClient.get<PurchaseOrderDto>(`/api/purchase-orders/${purchaseOrderId}`);
      setPoLookup(po);
    } catch (e) {
      setPoLookupError((e as Error).message);
    } finally {
      setPoLookingUp(false);
    }
  };

  const submitForm = async () => {
    setSubmitting(true);
    setFormError(null);
    try {
      if (editingId === null) {
        const payload: CreateAssetDto = {
          ...createForm,
          serialNumber: createForm.serialNumber?.trim() ? createForm.serialNumber : null,
        };
        await apiClient.post('/api/assets', payload);
      } else {
        await apiClient.put(`/api/assets/${editingId}`, editForm);
      }
      setDialogOpen(false);
      loadGrid();
    } catch (e) {
      setFormError((e as Error).message);
    } finally {
      setSubmitting(false);
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    setDeleteError(null);
    try {
      await apiClient.delete(`/api/assets/${deleteTarget.id}`);
      setDeleteTarget(null);
      loadGrid();
    } catch (e) {
      setDeleteError((e as Error).message);
    } finally {
      setDeleting(false);
    }
  };

  if (refDataError) return <Alert severity="error">{refDataError}</Alert>;
  if (!departments) return <CircularProgress />;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h5">Asset Registry</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
          New Asset
        </Button>
      </Box>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
          <TextField
            select
            label="Department"
            size="small"
            sx={{ minWidth: 180 }}
            value={departmentId}
            onChange={(e) => changeFilter(() => setDepartmentId(e.target.value === '' ? '' : Number(e.target.value)))}
          >
            <MenuItem value="">All Departments</MenuItem>
            {departments.map((d) => (
              <MenuItem key={d.id} value={d.id}>
                {d.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Status"
            size="small"
            sx={{ minWidth: 160 }}
            value={status}
            onChange={(e) => changeFilter(() => setStatus(e.target.value === '' ? '' : (Number(e.target.value) as AssetStatusValue)))}
          >
            <MenuItem value="">All Statuses</MenuItem>
            <MenuItem value={AssetStatus.InStock}>In Stock</MenuItem>
            <MenuItem value={AssetStatus.Assigned}>Assigned</MenuItem>
            <MenuItem value={AssetStatus.Maintenance}>Maintenance</MenuItem>
            <MenuItem value={AssetStatus.Retired}>Retired</MenuItem>
          </TextField>
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
              <TableCell>Asset Tag</TableCell>
              <TableCell>Serial Number</TableCell>
              <TableCell>Department</TableCell>
              <TableCell>PO</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Acquired</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            )}
            {!loading && grid?.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  No assets match these filters.
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              grid?.items.map((a) => (
                <TableRow key={a.id}>
                  <TableCell>{a.assetTag}</TableCell>
                  <TableCell>{a.serialNumber ?? <em>—</em>}</TableCell>
                  <TableCell>{departmentName(a.departmentId)}</TableCell>
                  <TableCell>#{a.purchaseOrderId}</TableCell>
                  <TableCell>
                    <Chip size="small" label={assetStatusLabel[a.status]} color={statusChipColor[a.status]} />
                  </TableCell>
                  <TableCell>{new Date(a.acquiredDate).toLocaleDateString()}</TableCell>
                  <TableCell align="right">
                    <Tooltip title="Edit">
                      <IconButton size="small" onClick={() => openEdit(a)}>
                        <EditIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete">
                      <IconButton size="small" onClick={() => setDeleteTarget(a)}>
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
        <TablePagination
          component="div"
          count={grid?.totalCount ?? 0}
          page={pageNumber - 1}
          onPageChange={(_e, newPage) => setPageNumber(newPage + 1)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(e) => {
            setPageSize(Number(e.target.value));
            setPageNumber(1);
          }}
          rowsPerPageOptions={[25, 50, 100]}
        />
      </TableContainer>

      <FormDialog
        open={dialogOpen}
        title={editingId === null ? 'New Asset' : 'Edit Asset'}
        onClose={() => setDialogOpen(false)}
        onSubmit={submitForm}
        submitting={submitting}
        error={formError}
      >
        {editingId === null && (
          <>
            <TextField
              type="number"
              label="Purchase Order Id"
              value={createForm.purchaseOrderId || ''}
              onChange={(e) => {
                const id = Number(e.target.value);
                setCreateForm({ ...createForm, purchaseOrderId: id });
              }}
              onBlur={() => lookupPo(createForm.purchaseOrderId)}
              required
              autoFocus
              helperText="The purchase order this asset arrived under"
            />
            {poLookingUp && <CircularProgress size={20} />}
            {poLookup && (
              <Alert severity="success" sx={{ py: 0 }}>
                {poLookup.poNumber} — qty {poLookup.quantity} — ${poLookup.totalCost.toFixed(2)}
              </Alert>
            )}
            {poLookupError && (
              <Alert severity="warning" sx={{ py: 0 }}>
                {poLookupError}
              </Alert>
            )}
            <TextField
              label="Asset Tag"
              value={createForm.assetTag}
              onChange={(e) => setCreateForm({ ...createForm, assetTag: e.target.value })}
              required
            />
            <TextField
              label="Serial Number"
              value={createForm.serialNumber ?? ''}
              onChange={(e) => setCreateForm({ ...createForm, serialNumber: e.target.value })}
            />
          </>
        )}
        <TextField
          select
          label="Department"
          value={editingId === null ? createForm.departmentId || '' : editForm.departmentId || ''}
          onChange={(e) =>
            editingId === null
              ? setCreateForm({ ...createForm, departmentId: Number(e.target.value) })
              : setEditForm({ ...editForm, departmentId: Number(e.target.value) })
          }
          required
        >
          {departments.map((d) => (
            <MenuItem key={d.id} value={d.id}>
              {d.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Status"
          value={editingId === null ? createForm.status : editForm.status}
          onChange={(e) => {
            const v = Number(e.target.value) as AssetStatusValue;
            editingId === null ? setCreateForm({ ...createForm, status: v }) : setEditForm({ ...editForm, status: v });
          }}
        >
          <MenuItem value={AssetStatus.InStock}>In Stock</MenuItem>
          <MenuItem value={AssetStatus.Assigned}>Assigned</MenuItem>
          <MenuItem value={AssetStatus.Maintenance}>Maintenance</MenuItem>
          <MenuItem value={AssetStatus.Retired}>Retired</MenuItem>
        </TextField>
      </FormDialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete asset?"
        message={`Delete asset "${deleteTarget?.assetTag}"? This can't be undone.`}
        onCancel={() => {
          setDeleteTarget(null);
          setDeleteError(null);
        }}
        onConfirm={confirmDelete}
        confirming={deleting}
        error={deleteError}
      />
    </Box>
  );
}
