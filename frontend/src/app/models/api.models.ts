export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Customer' | 'Admin';
  isActive: boolean;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: CurrentUser;
}

export type UnitOfMeasure = 'Piece' | 'Meter' | 'Pack' | 'Kilogram' | 'SquareMeter';

export interface Product {
  id: string;
  name: string;
  sku: string;
  brand: string;
  categoryId: string;
  categoryName: string;
  price: number;
  currentPrice: number;
  vatRate: number;
  discountPercentage: number;
  hasActiveDiscount: boolean;
  unitOfMeasure: UnitOfMeasure;
  unitsPerPackage: number;
  stockQuantity: number;
  imageUrl: string;
  isActive: boolean;
}

export interface ProductDetail extends Product {
  description: string;
  costPrice: number;
  discountStart: string | null;
  discountEnd: string | null;
  minimumStockLevel: number;
  createdAt: string;
  updatedAt: string;
}

export interface Category {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  productCount: number;
}

export interface ProductQuery {
  page: number;
  pageSize: number;
  search?: string;
  categoryId?: string;
  brand?: string;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
  discounted?: boolean;
  includeInactive?: boolean;
  sort?: 'NameAsc' | 'NameDesc' | 'PriceAsc' | 'PriceDesc' | 'Newest' | 'MostSold';
}

export interface CartItem {
  id: string;
  productId: string;
  productName: string;
  sku: string;
  imageUrl: string;
  unitOfMeasure: UnitOfMeasure;
  quantity: number;
  availableStock: number;
  unitPrice: number;
  vatRate: number;
  lineSubtotal: number;
  lineVat: number;
  lineTotal: number;
  productIsActive: boolean;
}

export interface Cart {
  id: string;
  items: CartItem[];
  distinctItemCount: number;
  subtotal: number;
  vatTotal: number;
  grandTotal: number;
  updatedAt: string;
}

export interface AnonymousCartItem {
  product: Product;
  quantity: number;
}

export type OrderStatus = 'PendingPayment' | 'Paid' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
export type PaymentStatus = 'Pending' | 'Paid' | 'Declined' | 'Refunded';

export interface OrderItem {
  productId: string;
  productName: string;
  sku: string;
  quantity: number;
  unitOfMeasure: UnitOfMeasure;
  unitPrice: number;
  vatRate: number;
  discountPercentage: number;
  lineSubtotal: number;
  lineVat: number;
  lineTotal: number;
}

export interface Order {
  id: string;
  orderNumber: string;
  userId: string;
  customerName: string;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  paymentMethod: 'CashOnDelivery' | 'MockCard';
  createdAt: string;
  paidAt: string | null;
  shippedAt: string | null;
  deliveredAt: string | null;
  subtotal: number;
  vatTotal: number;
  discountTotal: number;
  grandTotal: number;
  costTotal: number;
  contactName: string;
  contactEmail: string;
  shippingAddress: string;
  shippingCity: string;
  shippingPostalCode: string;
  items: OrderItem[];
}

export interface InventoryItem {
  productId: string;
  productName: string;
  sku: string;
  quantity: number;
  minimumStockLevel: number;
  isLowStock: boolean;
  updatedAt: string;
}

export interface Dashboard {
  from: string;
  to: string;
  totalRevenue: number;
  totalCost: number;
  grossProfit: number;
  netRevenue: number;
  netProfit: number;
  numberOfOrders: number;
  averageOrderValue: number;
  productsSold: number;
  lowStockProducts: InventoryItem[];
  revenueSeries: { date: string; revenue: number; cost: number }[];
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

