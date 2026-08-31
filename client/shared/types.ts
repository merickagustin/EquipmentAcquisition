// Mirrors EquipmentAcquisition.Core.Dtos.MenuItemDto and friends.
// Field names are camelCase — ASP.NET Core's default JSON naming policy.

export interface MenuItemDto {
  id: number;
  parentId: number | null;
  label: string;
  route: string | null;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateMenuItemDto {
  parentId: number | null;
  label: string;
  route: string | null;
  displayOrder: number;
  isActive: boolean;
}

export type UpdateMenuItemDto = CreateMenuItemDto;

// Mirrors the { status, detail } shape ExceptionHandlingMiddleware writes for
// 400/404/409 — see EquipmentAcquisition.Api/Middleware/ExceptionHandlingMiddleware.cs.
export interface ApiProblem {
  status: number;
  detail: string;
}
