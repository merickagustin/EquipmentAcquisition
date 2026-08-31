import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import { apiClient } from '../shared/apiClient';
import { ConfirmDialog } from '../shared/components/ConfirmDialog';
import { FormDialog } from '../shared/components/FormDialog';
import type { CreateVendorDto, VendorDto } from '../shared/types';

const emptyForm: CreateVendorDto = { name: '', contactEmail: '' };

export function VendorsAdminApp() {
  const [items, setItems] = useState<VendorDto[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<CreateVendorDto>(emptyForm);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<VendorDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const load = () => {
    setLoading(true);
    apiClient
      .get<VendorDto[]>('/api/vendors')
      .then((data) => {
        setItems(data);
        setLoadError(null);
      })
      .catch((e: Error) => setLoadError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm);
    setFormError(null);
    setDialogOpen(true);
  };

  const openEdit = (item: VendorDto) => {
    setEditingId(item.id);
    setForm({ name: item.name, contactEmail: item.contactEmail });
    setFormError(null);
    setDialogOpen(true);
  };

  const submitForm = async () => {
    setSubmitting(true);
    setFormError(null);
    try {
      const payload: CreateVendorDto = {
        ...form,
        contactEmail: form.contactEmail?.trim() ? form.contactEmail : null,
      };
      if (editingId === null) {
        await apiClient.post('/api/vendors', payload);
      } else {
        await apiClient.put(`/api/vendors/${editingId}`, payload);
      }
      setDialogOpen(false);
      load();
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
      await apiClient.delete(`/api/vendors/${deleteTarget.id}`);
      setDeleteTarget(null);
      load();
    } catch (e) {
      setDeleteError((e as Error).message);
    } finally {
      setDeleting(false);
    }
  };

  if (loadError && !items) return <Alert severity="error">{loadError}</Alert>;
  if (!items) return <CircularProgress />;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h5">Vendors</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
          New Vendor
        </Button>
      </Box>

      {loadError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {loadError}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Contact Email</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={3} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              items.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.name}</TableCell>
                  <TableCell>{item.contactEmail ?? <em>—</em>}</TableCell>
                  <TableCell align="right">
                    <IconButton size="small" onClick={() => openEdit(item)}>
                      <EditIcon fontSize="small" />
                    </IconButton>
                    <IconButton size="small" onClick={() => setDeleteTarget(item)}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
      </TableContainer>

      <FormDialog
        open={dialogOpen}
        title={editingId === null ? 'New Vendor' : 'Edit Vendor'}
        onClose={() => setDialogOpen(false)}
        onSubmit={submitForm}
        submitting={submitting}
        error={formError}
      >
        <TextField
          label="Name"
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
          required
          autoFocus
        />
        <TextField
          label="Contact Email"
          type="email"
          value={form.contactEmail ?? ''}
          onChange={(e) => setForm({ ...form, contactEmail: e.target.value })}
        />
      </FormDialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete vendor?"
        message={`Delete "${deleteTarget?.name}"? This can't be undone.`}
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
