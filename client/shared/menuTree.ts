import type { MenuItemDto } from './types';

export interface MenuTreeNode extends MenuItemDto {
  children: MenuTreeNode[];
}

export function buildTree(flat: MenuItemDto[]): MenuTreeNode[] {
  const byId = new Map<number, MenuTreeNode>();
  flat.forEach((item) => byId.set(item.id, { ...item, children: [] }));

  const roots: MenuTreeNode[] = [];
  byId.forEach((node) => {
    if (node.parentId === null) {
      roots.push(node);
      return;
    }
    const parent = byId.get(node.parentId);
    // Orphan safety net — shouldn't happen given the DB's self-referencing FK,
    // but surface it as a root rather than silently dropping it.
    if (parent) parent.children.push(node);
    else roots.push(node);
  });

  const sortByDisplayOrder = (nodes: MenuTreeNode[]) => {
    nodes.sort((a, b) => a.displayOrder - b.displayOrder);
    nodes.forEach((n) => sortByDisplayOrder(n.children));
  };
  sortByDisplayOrder(roots);
  return roots;
}

export interface FlatWithDepth extends MenuItemDto {
  depth: number;
}

// Used by menu-admin to indent its table rows — shows every row, active or not.
export function flattenWithDepth(tree: MenuTreeNode[], depth = 0): FlatWithDepth[] {
  const result: FlatWithDepth[] = [];
  for (const node of tree) {
    const { children, ...rest } = node;
    result.push({ ...rest, depth });
    result.push(...flattenWithDepth(children, depth + 1));
  }
  return result;
}

// A `nav` rule, not a database one — a node renders only if it AND every
// ancestor is active, so deactivating a group hides its children too instead
// of leaving them floating at top level. See table-design.md's MenuItem notes.
// A group (route === null) that's active but ends up with no active children
// after filtering is dropped too — an empty header with nothing under it is
// dead UI, not a useful "coming soon" signal.
export function filterActiveWithActiveAncestors(tree: MenuTreeNode[]): MenuTreeNode[] {
  return tree
    .filter((node) => node.isActive)
    .map((node) => ({ ...node, children: filterActiveWithActiveAncestors(node.children) }))
    .filter((node) => node.route !== null || node.children.length > 0);
}
