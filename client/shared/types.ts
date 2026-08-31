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

// Mirrors EquipmentAcquisition.Core.Dtos.EmployeeDto.
export interface EmployeeDto {
  id: number;
  departmentId: number;
  fullName: string;
  jobTitle: string | null;
}

// Mirrors EquipmentAcquisition.Domain.Enums.AcquisitionRequestStatus — serialized
// as its underlying byte (System.Text.Json's default), not a string.
export const AcquisitionRequestStatus = { Pending: 0, Approved: 1, Rejected: 2 } as const;
export type AcquisitionRequestStatusValue = 0 | 1 | 2;

export const acquisitionRequestStatusLabel: Record<AcquisitionRequestStatusValue, string> = {
  0: 'Pending',
  1: 'Approved',
  2: 'Rejected',
};

// Mirrors EquipmentAcquisition.Core.Dtos.AcquisitionRequestDto and friends.
export interface AcquisitionRequestDto {
  id: number;
  departmentId: number;
  equipmentCategoryId: number;
  requestedByEmployeeId: number;
  itemDescription: string;
  justification: string | null;
  quantity: number;
  estimatedCost: number;
  requestDate: string;
  approvedDate: string | null;
  rejectedDate: string | null;
  approvedByEmployeeId: number | null;
  rejectionReason: string | null;
  status: AcquisitionRequestStatusValue;
}

export interface CreateAcquisitionRequestDto {
  departmentId: number;
  equipmentCategoryId: number;
  requestedByEmployeeId: number;
  itemDescription: string;
  justification: string | null;
  quantity: number;
  estimatedCost: number;
}

export interface UpdateAcquisitionRequestDto {
  itemDescription: string;
  justification: string | null;
  quantity: number;
  estimatedCost: number;
}

export interface ApproveAcquisitionRequestDto {
  approvedByEmployeeId: number;
}

export interface RejectAcquisitionRequestDto {
  rejectionReason: string;
}

// Mirrors EquipmentAcquisition.Core.Dtos.RequestDetailDto — a grid row, read from
// EquipmentAcquisitionDetailCache, not the base tables. See table-design.md.
export interface RequestDetailDto {
  acquisitionRequestId: number;
  departmentName: string;
  equipmentCategoryName: string;
  requestedByName: string;
  approvedByName: string | null;
  itemDescription: string;
  quantity: number;
  estimatedCost: number;
  requestDate: string;
  status: AcquisitionRequestStatusValue;
  vendorName: string | null;
  totalCost: number | null;
  refreshedAt: string;
}

// Mirrors EquipmentAcquisition.Core.Dtos.RequestListQuery — DepartmentId, Status,
// From, To are mandatory (match the cache's composite index); the rest optional.
export interface RequestListQuery {
  departmentId: number;
  status: AcquisitionRequestStatusValue;
  from: string;
  to: string;
  equipmentCategoryId?: number;
  vendorId?: number;
  requestedByEmployeeId?: number;
  approvedByEmployeeId?: number;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

// Mirrors EquipmentAcquisition.Core.Dtos.PagedResult<T>.
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

// Mirrors EquipmentAcquisition.Core.Dtos.PurchaseOrderDto and friends.
export interface PurchaseOrderDto {
  id: number;
  acquisitionRequestId: number;
  vendorId: number;
  poNumber: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  orderDate: string;
}

export interface CreatePurchaseOrderDto {
  acquisitionRequestId: number;
  vendorId: number;
  poNumber: string;
  quantity: number;
  unitCost: number;
}

export interface UpdatePurchaseOrderDto {
  vendorId: number;
  quantity: number;
  unitCost: number;
}

// Mirrors EquipmentAcquisition.Core.Dtos.PurchaseOrderListQuery — all filters optional.
export interface PurchaseOrderListQuery {
  vendorId?: number;
  acquisitionRequestId?: number;
  pageNumber?: number;
  pageSize?: number;
}

// Mirrors EquipmentAcquisition.Domain.Enums.AssetStatus.
export const AssetStatus = { InStock: 0, Assigned: 1, Maintenance: 2, Retired: 3 } as const;
export type AssetStatusValue = 0 | 1 | 2 | 3;

export const assetStatusLabel: Record<AssetStatusValue, string> = {
  0: 'In Stock',
  1: 'Assigned',
  2: 'Maintenance',
  3: 'Retired',
};

// Mirrors EquipmentAcquisition.Core.Dtos.AssetDto and friends.
export interface AssetDto {
  id: number;
  purchaseOrderId: number;
  departmentId: number;
  assetTag: string;
  serialNumber: string | null;
  status: AssetStatusValue;
  acquiredDate: string;
  lastUpdated: string;
}

export interface CreateAssetDto {
  purchaseOrderId: number;
  departmentId: number;
  assetTag: string;
  serialNumber: string | null;
  status: AssetStatusValue;
}

export interface UpdateAssetDto {
  departmentId: number;
  status: AssetStatusValue;
}

// Mirrors EquipmentAcquisition.Core.Dtos.AssetListQuery — all filters optional.
export interface AssetListQuery {
  departmentId?: number;
  purchaseOrderId?: number;
  status?: AssetStatusValue;
  pageNumber?: number;
  pageSize?: number;
}

// Mirrors EquipmentAcquisition.Core.Dtos.ReportRowDto.
export interface ReportRowDto {
  departmentName: string;
  categoryName: string;
  requestCount: number;
  totalSpend: number;
}

// Mirrors the { status, detail } shape ExceptionHandlingMiddleware writes for
// 400/404/409 — see EquipmentAcquisition.Api/Middleware/ExceptionHandlingMiddleware.cs.
export interface ApiProblem {
  status: number;
  detail: string;
}
