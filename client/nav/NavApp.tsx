import React, { useEffect, useState } from 'react';
import { Box, CircularProgress, Collapse, List, ListItemButton, ListItemText } from '@mui/material';
import ExpandLess from '@mui/icons-material/ExpandLess';
import ExpandMore from '@mui/icons-material/ExpandMore';
import { apiClient } from '../shared/apiClient';
import { buildTree, filterActiveWithActiveAncestors, MenuTreeNode } from '../shared/menuTree';
import type { MenuItemDto } from '../shared/types';

function NavNode({ node, currentPath }: { node: MenuTreeNode; currentPath: string }) {
  const [open, setOpen] = useState(true); // groups default expanded — simplest useful behavior at this seed size
  const isGroup = node.route === null;

  if (isGroup) {
    return (
      <>
        <ListItemButton onClick={() => setOpen(!open)}>
          <ListItemText primary={node.label} />
          {open ? <ExpandLess /> : <ExpandMore />}
        </ListItemButton>
        <Collapse in={open} timeout="auto" unmountOnExit>
          <List component="div" disablePadding sx={{ pl: 2 }}>
            {node.children.map((child) => (
              <NavNode key={child.id} node={child} currentPath={currentPath} />
            ))}
          </List>
        </Collapse>
      </>
    );
  }

  return (
    <ListItemButton component="a" href={node.route ?? undefined} selected={node.route === currentPath}>
      <ListItemText primary={node.label} />
    </ListItemButton>
  );
}

export function NavApp() {
  const [tree, setTree] = useState<MenuTreeNode[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient
      .get<MenuItemDto[]>('/api/menu-items')
      .then((flat) => setTree(filterActiveWithActiveAncestors(buildTree(flat))))
      .catch((e: Error) => setError(e.message));
  }, []);

  if (error) {
    return (
      <Box sx={{ p: 2, color: 'error.main', fontSize: 14 }}>
        {error}
      </Box>
    );
  }

  if (!tree) {
    return (
      <Box sx={{ p: 2 }}>
        <CircularProgress size={24} />
      </Box>
    );
  }

  const currentPath = window.location.pathname;

  return (
    <List component="nav">
      {tree.map((node) => (
        <NavNode key={node.id} node={node} currentPath={currentPath} />
      ))}
    </List>
  );
}
