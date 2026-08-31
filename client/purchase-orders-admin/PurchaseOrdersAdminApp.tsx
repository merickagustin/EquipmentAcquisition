import React, { useEffect, useState } from 'react';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
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
import type {
  CreatePurchaseOrderDto,
  EligibleRequestDto,
  PagedResult,
  PurchaseOrderDto,
  UpdatePurchaseOrderDto,
  VendorDto,
} from '../shared/types';

// Full detail, not just "#123" — the whole point of the picker is to make it
// obvious which real-world item this PO is for before committing to it.
const eligibleRequestLabel = (r: EligibleRequestDto) =>
  `#${r.id} — ${r.itemDescription} — ${r.departmentName} — requested by ${r.requestedByName} — qty ${r.quantity} — $${r.estimatedCost.toFixed(2)}`;

const emptyCreateForm = { acquisitionRequestId: 0, vendorId: 0, poNumber: '', quantity: 1, unitCost: 0 };
const emptyEditForm: UpdatePurchaseOrderDto = { vendorId: 0, quantity: 1, unitCost: 0 };

export function PurchaseOrdersAdminApp() {
  const [vendors, setVendors] = useState<VendorDto[] | null>(null);
  const [refDataError, setRefDataError] = useState<string | null>(null);
  const vendorName = (id: number) => vendors?.find((v) => v.id === id)?.name ?? `#${id}`;

  const [vendorId, setVendorId] = useState<number | ''>('');
  const [acquisitionRequestId, setAcquisitionRequestId] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const [grid, setGrid] = useState<PagedResult<PurchaseOrderDto> | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [createForm, setCreateForm] = useState(emptyCreateForm);
  const [editForm, setEditForm] = useState<UpdatePurchaseOrderDto>(emptyEditForm);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [eligibleRequests, setEligibleRequests] = useState<EligibleRequestDto[] | null>(null);
  const [eligibleError, setEligibleError] = useState<string | null>(null);
  const [selectedRequest, setSelectedRequest] = useState<EligibleRequestDto | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<PurchaseOrderDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  useEffect(() => {
    apiClient.get<VendorDto[]>('/api/vendors').catch((e: Error) => {
      setRefDataError(e.message);
      return null;
    }).then((v) => v && setVendors(v));
  }, []);

  const loadGrid = () => {
    setLoading(true);
    const params = new URLSearchParams({ PageNumber: String(pageNumber), PageSize: String(pageSize) });
    if (vendorId !== '') params.set('VendorId', String(vendorId));
    if (acquisitionRequestId.trim()) params.set('AcquisitionRequestId', acquisitionRequestId.trim());
    apiClient
      .get<PagedResult<PurchaseOrderDto>>(`/api/purchase-orders/grid?${params.toString()}`)
      .then((data) => {
        setGrid(data);
        setLoadError(null);
      })
      .catch((e: Error) => setLoadError(e.message))
      .finally(() => setLoading(false));
  };

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(loadGrid, [vendorId, acquisitionRequestId, pageNumber, pageSize]);

  const changeFilter = (apply: () => void) => {
    apply();
    setPageNumber(1);
  };

  const openCreate = () => {
    setEditingId(null);
    setCreateForm({ ...emptyCreateForm, vendorId: typeof vendorId === 'number' ? vendorId : 0 });
    setSelectedRequest(null);
    setFormError(null);
    setDialogOpen(true);
    setEligibleRequests(null);
    setEligibleError(null);
    apiClient
      .get<EligibleRequestDto[]>('/api/purchase-orders/eligible-requests')
      .then(setEligibleRequests)
      .catch((e: Error) => setEligibleError(e.message));
  };

  const openEdit = (po: PurchaseOrderDto) => {
    setEditingId(po.id);
    setEditForm({ vendorId: po.vendorId, quantity: po.quantity, unitCost: po.unitCost });
    setFormError(null);
    setDialogOpen(true);
  };

  const submitForm = async () => {
    setSubmitting(true);
    setFormError(null);
    try {
      if (editingId === null) {
        const payload: CreatePurchaseOrderDto = createForm;
        await apiClient.post('/api/purchase-orders', payload);
      } else {
        await apiClient.put(`/api/purchase-orders/${editingId}`, editForm);
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
      await apiClient.delete(`/api/purchase-orders/${deleteTarget.id}`);
      setDeleteTarget(null);
      loadGrid();
    } catch (e) {
      setDeleteError((e as Error).message);
    } finally {
      setDeleting(false);
    }
  };

  if (refDataError) return <Alert severity="error">{refDataError}</Alert>;
  if (!vendors) return <CircularProgress />;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h5">Purchase Orders</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
          New Purchase Order
        </Button>
      </Box>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
          <TextField
            select
            label="Vendor"
            size="small"
            sx={{ minWidth: 200 }}
            value={vendorId}
            onChange={(e) => changeFilter(() => setVendorId(e.target.value === '' ? '' : Number(e.target.value)))}
          >
            <MenuItem value="">All Vendors</MenuItem>
            {vendors.map((v) => (
              <MenuItem key={v.id} value={v.id}>
                {v.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            type="number"
            label="Acquisition Request Id"
            size="small"
            value={acquisitionRequestId}
            onChange={(e) => changeFilter(() => setAcquisitionRequestId(e.target.value))}
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
              <TableCell>PO Number</TableCell>
              <TableCell>Request</TableCell>
              <TableCell>Vendor</TableCell>
              <TableCell align="right">Qty</TableCell>
              <TableCell align="right">Unit Cost</TableCell>
              <TableCell align="right">Total Cost</TableCell>
              <TableCell>Order Date</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={8} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            )}
            {!loading && grid?.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={8} align="center">
                  No purchase orders match these filters.
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              grid?.items.map((po) => (
                <TableRow key={po.id}>
                  <TableCell>{po.poNumber}</TableCell>
                  <TableCell>#{po.acquisitionRequestId}</TableCell>
                  <TableCell>{vendorName(po.vendorId)}</TableCell>
                  <TableCell align="right">{po.quantity}</TableCell>
                  <TableCell align="right">${po.unitCost.toFixed(2)}</TableCell>
                  <TableCell align="right">${po.totalCost.toFixed(2)}</TableCell>
                  <TableCell>{new Date(po.orderDate).toLocaleDateString()}</TableCell>
                  <TableCell align="right">
                    <Tooltip title="Edit">
                      <IconButton size="small" onClick={() => openEdit(po)}>
                        <EditIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete">
                      <IconButton size="small" onClick={() => setDeleteTarget(po)}>
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
        title={editingId === null ? 'New Purchase Order' : 'Edit Purchase Order'}
        onClose={() => setDialogOpen(false)}
        onSubmit={submitForm}
        submitting={submitting}
        error={formError}
      >
        {editingId === null && (
          <>
            {eligibleError && <Alert severity="error">{eligibleError}</Alert>}
            <Autocomplete
              options={eligibleRequests ?? []}
              loading={eligibleRequests === null}
              value={selectedRequest}
              getOptionLabel={eligibleRequestLabel}
              isOptionEqualToValue={(a, b) => a.id === b.id}
              onChange={(_e, value) => {
                setSelectedRequest(value);
                setCreateForm({
                  ...createForm,
                  acquisitionRequestId: value?.id ?? 0,
                  quantity: value?.quantity ?? 1,
                });
              }}
              noOptionsText={eligibleRequests === null ? 'Loading…' : 'No Approved requests without a purchase order'}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Acquisition Request"
                  required
                  autoFocus
                  helperText="Approved requests with no purchase order yet"
                  InputProps={{
                    ...params.InputProps,
                    endAdornment: (
                      <>
                        {eligibleRequests === null && <CircularProgress size={16} />}
                        {params.InputProps.endAdornment}
                      </>
                    ),
                  }}
                />
              )}
            />
            <TextField
              label="PO Number"
              value={createForm.poNumber}
              onChange={(e) => setCreateForm({ ...createForm, poNumber: e.target.value })}
              required
            />
          </>
        )}
        <TextField
          select
          label="Vendor"
          value={editingId === null ? createForm.vendorId || '' : editForm.vendorId || ''}
          onChange={(e) =>
            editingId === null
              ? setCreateForm({ ...createForm, vendorId: Number(e.target.value) })
              : setEditForm({ ...editForm, vendorId: Number(e.target.value) })
          }
          required
        >
          {vendors.map((v) => (
            <MenuItem key={v.id} value={v.id}>
              {v.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          type="number"
          label="Quantity"
          value={editingId === null ? createForm.quantity : editForm.quantity}
          onChange={(e) => {
            const v = Number(e.target.value);
            editingId === null ? setCreateForm({ ...createForm, quantity: v }) : setEditForm({ ...editForm, quantity: v });
          }}
        />
        <TextField
          type="number"
          label="Unit Cost"
          value={editingId === null ? createForm.unitCost : editForm.unitCost}
          onChange={(e) => {
            const v = Number(e.target.value);
            editingId === null ? setCreateForm({ ...createForm, unitCost: v }) : setEditForm({ ...editForm, unitCost: v });
          }}
        />
      </FormDialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete purchase order?"
        message={`Delete "${deleteTarget?.poNumber}"? This can't be undone.`}
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
