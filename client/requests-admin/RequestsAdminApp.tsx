import React, { useEffect, useState } from 'react';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  MenuItem,
  Paper,
  Snackbar,
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
import CheckIcon from '@mui/icons-material/Check';
import CloseIcon from '@mui/icons-material/Close';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import RefreshIcon from '@mui/icons-material/Refresh';
import ShoppingCartIcon from '@mui/icons-material/ShoppingCart';
import { apiClient } from '../shared/apiClient';
import { ConfirmDialog } from '../shared/components/ConfirmDialog';
import { FormDialog } from '../shared/components/FormDialog';
import {
  AcquisitionRequestStatus,
  acquisitionRequestStatusLabel,
  type AcquisitionRequestDto,
  type AcquisitionRequestStatusValue,
  type CreateAcquisitionRequestDto,
  type CreatePurchaseOrderDto,
  type DepartmentDto,
  type EmployeeDto,
  type EquipmentCategoryDto,
  type PagedResult,
  type PurchaseOrderDto,
  type RequestDetailDto,
  type UpdateAcquisitionRequestDto,
  type UpdatePurchaseOrderDto,
  type VendorDto,
} from '../shared/types';

function isoDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}

function defaultFrom(): string {
  const d = new Date();
  d.setFullYear(d.getFullYear() - 3);
  return isoDate(d);
}

const emptyCreateForm: CreateAcquisitionRequestDto = {
  departmentId: 0,
  equipmentCategoryId: 0,
  requestedByEmployeeId: 0,
  itemDescription: '',
  justification: '',
  quantity: 1,
  estimatedCost: 0,
};

const emptyEditForm: UpdateAcquisitionRequestDto = {
  itemDescription: '',
  justification: '',
  quantity: 1,
  estimatedCost: 0,
};

const emptyPoForm = { vendorId: 0, quantity: 1, unitCost: 0 };

const statusChipColor: Record<AcquisitionRequestStatusValue, 'warning' | 'success' | 'error'> = {
  0: 'warning',
  1: 'success',
  2: 'error',
};

export function RequestsAdminApp() {
  const [departments, setDepartments] = useState<DepartmentDto[] | null>(null);
  const [categories, setCategories] = useState<EquipmentCategoryDto[] | null>(null);
  const [employees, setEmployees] = useState<EmployeeDto[] | null>(null);
  const [vendors, setVendors] = useState<VendorDto[] | null>(null);
  const [refDataError, setRefDataError] = useState<string | null>(null);

  const [departmentId, setDepartmentId] = useState<number | ''>('');
  const [status, setStatus] = useState<AcquisitionRequestStatusValue>(AcquisitionRequestStatus.Pending);
  const [from, setFrom] = useState(defaultFrom());
  const [to, setTo] = useState(isoDate(new Date()));
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [grid, setGrid] = useState<PagedResult<RequestDetailDto> | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [createForm, setCreateForm] = useState<CreateAcquisitionRequestDto>(emptyCreateForm);
  const [editForm, setEditForm] = useState<UpdateAcquisitionRequestDto>(emptyEditForm);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [approveTarget, setApproveTarget] = useState<RequestDetailDto | null>(null);
  const [approverId, setApproverId] = useState<number | null>(null);
  const [approving, setApproving] = useState(false);
  const [approveError, setApproveError] = useState<string | null>(null);

  const [rejectTarget, setRejectTarget] = useState<RequestDetailDto | null>(null);
  const [rejectReason, setRejectReason] = useState('');
  const [rejecting, setRejecting] = useState(false);
  const [rejectError, setRejectError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<RequestDetailDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const [poTarget, setPoTarget] = useState<RequestDetailDto | null>(null);
  const [poEditingId, setPoEditingId] = useState<number | null>(null);
  const [poForm, setPoForm] = useState(emptyPoForm);
  const [existingPoNumber, setExistingPoNumber] = useState<string | null>(null);
  const [poLoading, setPoLoading] = useState(false);
  const [poSubmitting, setPoSubmitting] = useState(false);
  const [poError, setPoError] = useState<string | null>(null);
  const [poDeleteConfirm, setPoDeleteConfirm] = useState(false);
  const [poDeleting, setPoDeleting] = useState(false);

  useEffect(() => {
    Promise.all([
      apiClient.get<DepartmentDto[]>('/api/departments'),
      apiClient.get<EquipmentCategoryDto[]>('/api/equipment-categories'),
      apiClient.get<EmployeeDto[]>('/api/employees'),
      apiClient.get<VendorDto[]>('/api/vendors'),
    ])
      .then(([d, c, e, v]) => {
        setDepartments(d);
        setCategories(c);
        setEmployees(e);
        setVendors(v);
        if (d.length > 0) setDepartmentId(d[0].id);
      })
      .catch((e: Error) => setRefDataError(e.message));
  }, []);

  const loadGrid = () => {
    if (departmentId === '') return;
    setLoading(true);
    const params = new URLSearchParams({
      DepartmentId: String(departmentId),
      Status: String(status),
      From: from,
      // End-of-day, not the bare date — a bare "2026-08-31" binds to midnight,
      // which would exclude every request from that day created after 00:00:00.
      To: `${to}T23:59:59.999`,
      PageNumber: String(pageNumber),
      PageSize: String(pageSize),
    });
    apiClient
      .get<PagedResult<RequestDetailDto>>(`/api/acquisition-requests/grid?${params.toString()}`)
      .then((data) => {
        setGrid(data);
        setLoadError(null);
      })
      .catch((e: Error) => setLoadError(e.message))
      .finally(() => setLoading(false));
  };

  // The grid reads EquipmentAcquisitionDetailCache, not the base tables — a mutation
  // enqueues a refresh but DetailCacheRefreshWorker only drains that queue every 2s
  // (see table-design.md's orchestration section), so an immediate reload right after
  // a mutation can still read stale cache. Rather than guess at a delay, say so and
  // let the Refresh button be the retry — honest about the trade-off instead of
  // masking it with a timer.
  const notifyAndReload = (message: string) => {
    loadGrid();
    setNotice(message);
  };

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(loadGrid, [departmentId, status, from, to, pageNumber, pageSize]);

  const changeFilter = (apply: () => void) => {
    apply();
    setPageNumber(1);
  };

  const openCreate = () => {
    setEditingId(null);
    setCreateForm({ ...emptyCreateForm, departmentId: typeof departmentId === 'number' ? departmentId : 0 });
    setFormError(null);
    setDialogOpen(true);
  };

  const openEdit = async (row: RequestDetailDto) => {
    setFormError(null);
    try {
      const full = await apiClient.get<AcquisitionRequestDto>(`/api/acquisition-requests/${row.acquisitionRequestId}`);
      setEditingId(row.acquisitionRequestId);
      setEditForm({
        itemDescription: full.itemDescription,
        justification: full.justification,
        quantity: full.quantity,
        estimatedCost: full.estimatedCost,
      });
      setDialogOpen(true);
    } catch (e) {
      setLoadError((e as Error).message);
    }
  };

  const submitForm = async () => {
    setSubmitting(true);
    setFormError(null);
    try {
      if (editingId === null) {
        const payload: CreateAcquisitionRequestDto = {
          ...createForm,
          justification: createForm.justification?.trim() ? createForm.justification : null,
        };
        await apiClient.post('/api/acquisition-requests', payload);
      } else {
        const payload: UpdateAcquisitionRequestDto = {
          ...editForm,
          justification: editForm.justification?.trim() ? editForm.justification : null,
        };
        await apiClient.put(`/api/acquisition-requests/${editingId}`, payload);
      }
      setDialogOpen(false);
      notifyAndReload(
        editingId === null
          ? 'Request created — it can take a few seconds to appear below.'
          : 'Request updated — it can take a few seconds to reflect below.',
      );
    } catch (e) {
      setFormError((e as Error).message);
    } finally {
      setSubmitting(false);
    }
  };

  const submitApprove = async () => {
    if (!approveTarget || approverId === null) return;
    setApproving(true);
    setApproveError(null);
    try {
      await apiClient.post(`/api/acquisition-requests/${approveTarget.acquisitionRequestId}/approve`, {
        approvedByEmployeeId: approverId,
      });
      setApproveTarget(null);
      notifyAndReload('Request approved — it can take a few seconds to reflect below.');
    } catch (e) {
      setApproveError((e as Error).message);
    } finally {
      setApproving(false);
    }
  };

  const submitReject = async () => {
    if (!rejectTarget) return;
    setRejecting(true);
    setRejectError(null);
    try {
      await apiClient.post(`/api/acquisition-requests/${rejectTarget.acquisitionRequestId}/reject`, {
        rejectionReason: rejectReason,
      });
      setRejectTarget(null);
      notifyAndReload('Request rejected — it can take a few seconds to reflect below.');
    } catch (e) {
      setRejectError((e as Error).message);
    } finally {
      setRejecting(false);
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    setDeleteError(null);
    try {
      await apiClient.delete(`/api/acquisition-requests/${deleteTarget.acquisitionRequestId}`);
      setDeleteTarget(null);
      notifyAndReload('Request deleted — it can take a few seconds to reflect below.');
    } catch (e) {
      setDeleteError((e as Error).message);
    } finally {
      setDeleting(false);
    }
  };

  const openPoDialog = async (row: RequestDetailDto) => {
    setPoTarget(row);
    setPoError(null);
    setPoEditingId(null);
    setPoForm({ ...emptyPoForm, quantity: row.quantity });
    setExistingPoNumber(null);
    setPoLoading(true);
    try {
      const existing = await apiClient.get<PurchaseOrderDto | null>(
        `/api/purchase-orders/by-request/${row.acquisitionRequestId}`,
      );
      if (existing) {
        setPoEditingId(existing.id);
        setExistingPoNumber(existing.poNumber);
        setPoForm({
          vendorId: existing.vendorId,
          quantity: existing.quantity,
          unitCost: existing.unitCost,
        });
      }
    } catch (e) {
      setPoError((e as Error).message);
    } finally {
      setPoLoading(false);
    }
  };

  const submitPo = async () => {
    if (!poTarget) return;
    setPoSubmitting(true);
    setPoError(null);
    try {
      if (poEditingId === null) {
        const payload: CreatePurchaseOrderDto = { acquisitionRequestId: poTarget.acquisitionRequestId, ...poForm };
        await apiClient.post('/api/purchase-orders', payload);
      } else {
        const payload: UpdatePurchaseOrderDto = {
          vendorId: poForm.vendorId,
          quantity: poForm.quantity,
          unitCost: poForm.unitCost,
        };
        await apiClient.put(`/api/purchase-orders/${poEditingId}`, payload);
      }
      setPoTarget(null);
      notifyAndReload(
        poEditingId === null
          ? 'Purchase order created — it can take a few seconds to reflect below.'
          : 'Purchase order updated — it can take a few seconds to reflect below.',
      );
    } catch (e) {
      setPoError((e as Error).message);
    } finally {
      setPoSubmitting(false);
    }
  };

  const confirmPoDelete = async () => {
    if (poEditingId === null) return;
    setPoDeleting(true);
    try {
      await apiClient.delete(`/api/purchase-orders/${poEditingId}`);
      setPoDeleteConfirm(false);
      setPoTarget(null);
      notifyAndReload('Purchase order removed — it can take a few seconds to reflect below.');
    } catch (e) {
      setPoError((e as Error).message);
    } finally {
      setPoDeleting(false);
    }
  };

  const refDataReady = departments && categories && employees && vendors;

  if (refDataError) return <Alert severity="error">{refDataError}</Alert>;
  if (!refDataReady) return <CircularProgress />;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h5">Acquisition Requests</Typography>
        <Stack direction="row" spacing={1}>
          <Button startIcon={<RefreshIcon />} onClick={loadGrid} disabled={loading}>
            Refresh
          </Button>
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
            New Request
          </Button>
        </Stack>
      </Box>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
          <TextField
            select
            label="Department"
            size="small"
            sx={{ minWidth: 180 }}
            value={departmentId}
            onChange={(e) => changeFilter(() => setDepartmentId(Number(e.target.value)))}
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
            size="small"
            sx={{ minWidth: 140 }}
            value={status}
            onChange={(e) => changeFilter(() => setStatus(Number(e.target.value) as AcquisitionRequestStatusValue))}
          >
            <MenuItem value={AcquisitionRequestStatus.Pending}>Pending</MenuItem>
            <MenuItem value={AcquisitionRequestStatus.Approved}>Approved</MenuItem>
            <MenuItem value={AcquisitionRequestStatus.Rejected}>Rejected</MenuItem>
          </TextField>
          <TextField
            type="date"
            label="From"
            size="small"
            InputLabelProps={{ shrink: true }}
            value={from}
            onChange={(e) => changeFilter(() => setFrom(e.target.value))}
          />
          <TextField
            type="date"
            label="To"
            size="small"
            InputLabelProps={{ shrink: true }}
            value={to}
            onChange={(e) => changeFilter(() => setTo(e.target.value))}
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
              <TableCell>Item</TableCell>
              <TableCell>Category</TableCell>
              <TableCell>Requested By</TableCell>
              <TableCell align="right">Qty</TableCell>
              <TableCell align="right">Est. Cost</TableCell>
              <TableCell>Request Date</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Vendor</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={9} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            )}
            {!loading && grid?.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={9} align="center">
                  No requests match these filters.
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              grid?.items.map((row) => (
                <TableRow key={row.acquisitionRequestId}>
                  <TableCell>{row.itemDescription}</TableCell>
                  <TableCell>{row.equipmentCategoryName}</TableCell>
                  <TableCell>{row.requestedByName}</TableCell>
                  <TableCell align="right">{row.quantity}</TableCell>
                  <TableCell align="right">${row.estimatedCost.toFixed(2)}</TableCell>
                  <TableCell>{new Date(row.requestDate).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={acquisitionRequestStatusLabel[row.status]}
                      color={statusChipColor[row.status]}
                    />
                  </TableCell>
                  <TableCell>{row.vendorName ?? <em>—</em>}</TableCell>
                  <TableCell align="right">
                    {row.status === AcquisitionRequestStatus.Pending && (
                      <>
                        <Tooltip title="Edit">
                          <IconButton size="small" onClick={() => openEdit(row)}>
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Approve">
                          <IconButton
                            size="small"
                            color="success"
                            onClick={() => {
                              setApproveTarget(row);
                              setApproverId(null);
                              setApproveError(null);
                            }}
                          >
                            <CheckIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Reject">
                          <IconButton
                            size="small"
                            color="error"
                            onClick={() => {
                              setRejectTarget(row);
                              setRejectReason('');
                              setRejectError(null);
                            }}
                          >
                            <CloseIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </>
                    )}
                    {row.status === AcquisitionRequestStatus.Approved && (
                      <Tooltip title={row.vendorName ? 'Edit Purchase Order' : 'Create Purchase Order'}>
                        <IconButton size="small" onClick={() => openPoDialog(row)}>
                          <ShoppingCartIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                    <Tooltip title="Delete">
                      <IconButton size="small" onClick={() => setDeleteTarget(row)}>
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
          rowsPerPageOptions={[10, 25, 50]}
        />
      </TableContainer>

      {/* Create / Edit */}
      <FormDialog
        open={dialogOpen}
        title={editingId === null ? 'New Request' : 'Edit Request'}
        onClose={() => setDialogOpen(false)}
        onSubmit={submitForm}
        submitting={submitting}
        error={formError}
      >
        {editingId === null && (
          <>
            <TextField
              select
              label="Department"
              value={createForm.departmentId || ''}
              onChange={(e) => setCreateForm({ ...createForm, departmentId: Number(e.target.value) })}
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
              label="Equipment Category"
              value={createForm.equipmentCategoryId || ''}
              onChange={(e) => setCreateForm({ ...createForm, equipmentCategoryId: Number(e.target.value) })}
              required
            >
              {categories.map((c) => (
                <MenuItem key={c.id} value={c.id}>
                  {c.name}
                </MenuItem>
              ))}
            </TextField>
            <Autocomplete
              options={employees}
              getOptionLabel={(e) => e.fullName}
              onChange={(_e, value) => setCreateForm({ ...createForm, requestedByEmployeeId: value?.id ?? 0 })}
              renderInput={(params) => <TextField {...params} label="Requested By" required autoFocus />}
            />
          </>
        )}
        <TextField
          label="Item Description"
          value={editingId === null ? createForm.itemDescription : editForm.itemDescription}
          onChange={(e) =>
            editingId === null
              ? setCreateForm({ ...createForm, itemDescription: e.target.value })
              : setEditForm({ ...editForm, itemDescription: e.target.value })
          }
          required
        />
        <TextField
          label="Justification"
          multiline
          minRows={2}
          value={(editingId === null ? createForm.justification : editForm.justification) ?? ''}
          onChange={(e) =>
            editingId === null
              ? setCreateForm({ ...createForm, justification: e.target.value })
              : setEditForm({ ...editForm, justification: e.target.value })
          }
        />
        <TextField
          type="number"
          label="Quantity"
          value={editingId === null ? createForm.quantity : editForm.quantity}
          onChange={(e) =>
            editingId === null
              ? setCreateForm({ ...createForm, quantity: Number(e.target.value) })
              : setEditForm({ ...editForm, quantity: Number(e.target.value) })
          }
        />
        <TextField
          type="number"
          label="Estimated Cost"
          value={editingId === null ? createForm.estimatedCost : editForm.estimatedCost}
          onChange={(e) =>
            editingId === null
              ? setCreateForm({ ...createForm, estimatedCost: Number(e.target.value) })
              : setEditForm({ ...editForm, estimatedCost: Number(e.target.value) })
          }
        />
      </FormDialog>

      {/* Approve */}
      <FormDialog
        open={approveTarget !== null}
        title={`Approve "${approveTarget?.itemDescription}"?`}
        onClose={() => setApproveTarget(null)}
        onSubmit={submitApprove}
        submitting={approving}
        error={approveError}
        submitLabel="Approve"
      >
        <Autocomplete
          options={employees}
          getOptionLabel={(e) => e.fullName}
          onChange={(_e, value) => setApproverId(value?.id ?? null)}
          renderInput={(params) => <TextField {...params} label="Approved By" required autoFocus />}
        />
      </FormDialog>

      {/* Reject */}
      <FormDialog
        open={rejectTarget !== null}
        title={`Reject "${rejectTarget?.itemDescription}"?`}
        onClose={() => setRejectTarget(null)}
        onSubmit={submitReject}
        submitting={rejecting}
        error={rejectError}
        submitLabel="Reject"
      >
        <TextField
          label="Rejection Reason"
          multiline
          minRows={2}
          value={rejectReason}
          onChange={(e) => setRejectReason(e.target.value)}
          required
          autoFocus
        />
      </FormDialog>

      {/* Delete */}
      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete request?"
        message={`Delete "${deleteTarget?.itemDescription}"? This can't be undone.`}
        onCancel={() => {
          setDeleteTarget(null);
          setDeleteError(null);
        }}
        onConfirm={confirmDelete}
        confirming={deleting}
        error={deleteError}
      />

      {/* Purchase Order */}
      <FormDialog
        open={poTarget !== null}
        title={poEditingId === null ? 'Create Purchase Order' : 'Edit Purchase Order'}
        onClose={() => setPoTarget(null)}
        onSubmit={submitPo}
        submitting={poSubmitting}
        error={poError}
      >
        {poLoading ? (
          <CircularProgress size={24} />
        ) : (
          <>
            <TextField
              select
              label="Vendor"
              value={poForm.vendorId || ''}
              onChange={(e) => setPoForm({ ...poForm, vendorId: Number(e.target.value) })}
              required
            >
              {vendors.map((v) => (
                <MenuItem key={v.id} value={v.id}>
                  {v.name}
                </MenuItem>
              ))}
            </TextField>
            {existingPoNumber !== null && (
              <Typography variant="body2" color="text.secondary">
                PO Number: {existingPoNumber} (generated, not editable)
              </Typography>
            )}
            <TextField
              type="number"
              label="Quantity"
              value={poForm.quantity}
              onChange={(e) => setPoForm({ ...poForm, quantity: Number(e.target.value) })}
            />
            <TextField
              type="number"
              label="Unit Cost"
              value={poForm.unitCost}
              onChange={(e) => setPoForm({ ...poForm, unitCost: Number(e.target.value) })}
            />
            <Typography variant="body2" color="text.secondary">
              Total: ${(poForm.quantity * poForm.unitCost).toFixed(2)}
            </Typography>
            {poEditingId !== null && (
              <Button color="error" size="small" onClick={() => setPoDeleteConfirm(true)} sx={{ alignSelf: 'flex-start' }}>
                Remove Purchase Order
              </Button>
            )}
          </>
        )}
      </FormDialog>

      <ConfirmDialog
        open={poDeleteConfirm}
        title="Remove purchase order?"
        message="Delete this purchase order? This can't be undone."
        onCancel={() => setPoDeleteConfirm(false)}
        onConfirm={confirmPoDelete}
        confirming={poDeleting}
        error={null}
      />

      <Snackbar
        open={notice !== null}
        autoHideDuration={6000}
        onClose={() => setNotice(null)}
        message={notice}
        action={
          <Button
            color="inherit"
            size="small"
            onClick={() => {
              loadGrid();
              setNotice(null);
            }}
          >
            Refresh
          </Button>
        }
      />
    </Box>
  );
}
