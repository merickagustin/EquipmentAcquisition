import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  FormControlLabel,
  IconButton,
  Paper,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  MenuItem as MuiMenuItem,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import { apiClient } from '../shared/apiClient';
import { ConfirmDialog } from '../shared/components/ConfirmDialog';
import { FormDialog } from '../shared/components/FormDialog';
import { buildTree, flattenWithDepth, FlatWithDepth } from '../shared/menuTree';
import type { CreateMenuItemDto, MenuItemDto } from '../shared/types';

const emptyForm: CreateMenuItemDto = { parentId: null, label: '', route: '', displayOrder: 1, isActive: true };

export function MenuAdminApp() {
  const [items, setItems] = useState<MenuItemDto[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<CreateMenuItemDto>(emptyForm);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<MenuItemDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const load = () => {
    setLoading(true);
    apiClient
      .get<MenuItemDto[]>('/api/menu-items')
      .then((data) => {
        setItems(data);
        setLoadError(null);
      })
      .catch((e: Error) => setLoadError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const rows: FlatWithDepth[] = items ? flattenWithDepth(buildTree(items)) : [];

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm);
    setFormError(null);
    setDialogOpen(true);
  };

  const openEdit = (item: MenuItemDto) => {
    setEditingId(item.id);
    setForm({
      parentId: item.parentId,
      label: item.label,
      route: item.route,
      displayOrder: item.displayOrder,
      isActive: item.isActive,
    });
    setFormError(null);
    setDialogOpen(true);
  };

  const submitForm = async () => {
    setSubmitting(true);
    setFormError(null);
    try {
      const payload: CreateMenuItemDto = { ...form, route: form.route?.trim() ? form.route : null };
      if (editingId === null) {
        await apiClient.post('/api/menu-items', payload);
      } else {
        await apiClient.put(`/api/menu-items/${editingId}`, payload);
      }
      setDialogOpen(false);
      load();
    } catch (e) {
      setFormError((e as Error).message);
    } finally {
      setSubmitting(false);
    }
  };

  // Optimistic — flips immediately, rolls back on failure. This is the toggle
  // that makes the "watch nav grow a branch" demo fast to perform.
  const toggleActive = async (item: MenuItemDto) => {
    const previous = items!;
    setItems(previous.map((i) => (i.id === item.id ? { ...i, isActive: !i.isActive } : i)));
    try {
      await apiClient.put(`/api/menu-items/${item.id}`, {
        parentId: item.parentId,
        label: item.label,
        route: item.route,
        displayOrder: item.displayOrder,
        isActive: !item.isActive,
      });
    } catch (e) {
      setItems(previous);
      setLoadError((e as Error).message);
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    setDeleteError(null);
    try {
      await apiClient.delete(`/api/menu-items/${deleteTarget.id}`);
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
        <Typography variant="h5">Menu Admin</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
          New Menu Item
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
              <TableCell>Label</TableCell>
              <TableCell>Route</TableCell>
              <TableCell align="right">Display Order</TableCell>
              <TableCell align="center">Active</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              rows.map((row) => (
                <TableRow key={row.id}>
                  <TableCell sx={{ pl: 2 + row.depth * 3 }}>{row.label}</TableCell>
                  <TableCell>{row.route ?? <em>— group —</em>}</TableCell>
                  <TableCell align="right">{row.displayOrder}</TableCell>
                  <TableCell align="center">
                    <Switch checked={row.isActive} onChange={() => toggleActive(row)} size="small" />
                  </TableCell>
                  <TableCell align="right">
                    <IconButton size="small" onClick={() => openEdit(row)}>
                      <EditIcon fontSize="small" />
                    </IconButton>
                    <IconButton size="small" onClick={() => setDeleteTarget(row)}>
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
        title={editingId === null ? 'New Menu Item' : 'Edit Menu Item'}
        onClose={() => setDialogOpen(false)}
        onSubmit={submitForm}
        submitting={submitting}
        error={formError}
      >
        <TextField
          label="Label"
          value={form.label}
          onChange={(e) => setForm({ ...form, label: e.target.value })}
          required
          autoFocus
        />
        <TextField
          label="Route"
          value={form.route ?? ''}
          onChange={(e) => setForm({ ...form, route: e.target.value })}
          helperText="Leave blank for a group header"
        />
        <TextField
          select
          label="Parent"
          value={form.parentId ?? ''}
          onChange={(e) => setForm({ ...form, parentId: e.target.value === '' ? null : Number(e.target.value) })}
        >
          <MuiMenuItem value="">— top level —</MuiMenuItem>
          {items
            .filter((i) => i.id !== editingId)
            .map((i) => (
              <MuiMenuItem key={i.id} value={i.id}>
                {i.label}
              </MuiMenuItem>
            ))}
        </TextField>
        <TextField
          type="number"
          label="Display Order"
          value={form.displayOrder}
          onChange={(e) => setForm({ ...form, displayOrder: Number(e.target.value) })}
        />
        <FormControlLabel
          control={<Checkbox checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />}
          label="Active"
        />
      </FormDialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete menu item?"
        message={`Delete "${deleteTarget?.label}"? This can't be undone.`}
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
