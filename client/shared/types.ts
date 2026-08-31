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

// Mirrors EquipmentAcquisition.Core.Dtos.VendorDto and friends.
export interface VendorDto {
  id: number;
  name: string;
  contactEmail: string | null;
}

export interface CreateVendorDto {
  name: string;
  contactEmail: string | null;
}

export type UpdateVendorDto = CreateVendorDto;

// Mirrors EquipmentAcquisition.Core.Dtos.DepartmentDto and friends.
export interface DepartmentDto {
  id: number;
  code: string;
  name: string;
}

export interface CreateDepartmentDto {
  code: string;
  name: string;
}

export type UpdateDepartmentDto = CreateDepartmentDto;

// Mirrors EquipmentAcquisition.Core.Dtos.EquipmentCategoryDto and friends.
export interface EquipmentCategoryDto {
  id: number;
  name: string;
}

export interface CreateEquipmentCategoryDto {
  name: string;
}

export type UpdateEquipmentCategoryDto = CreateEquipmentCategoryDto;

// Mirrors the { status, detail } shape ExceptionHandlingMiddleware writes for
// 400/404/409 — see EquipmentAcquisition.Api/Middleware/ExceptionHandlingMiddleware.cs.
export interface ApiProblem {
  status: number;
  detail: string;
}
