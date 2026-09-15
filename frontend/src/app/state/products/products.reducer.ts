import { EntityState, createEntityAdapter } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { Category, Product } from '../../models/api.models';
import { ProductsActions } from './products.actions';

export interface ProductsState extends EntityState<Product> {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  categories: Category[];
  loading: boolean;
  error: string | null;
}

export const productsAdapter = createEntityAdapter<Product>({ sortComparer: (a, b) => a.name.localeCompare(b.name) });
const initialState = productsAdapter.getInitialState({
  page: 1, pageSize: 20, totalCount: 0, totalPages: 0, categories: [], loading: false, error: null
});

export const productsReducer = createReducer(
  initialState,
  on(ProductsActions.load, state => ({ ...state, loading: true, error: null })),
  on(ProductsActions.loadSuccess, (state, { result }) => productsAdapter.setAll(result.items, {
    ...state,
    page: result.page,
    pageSize: result.pageSize,
    totalCount: result.totalCount,
    totalPages: result.totalPages,
    loading: false
  })),
  on(ProductsActions.loadFailure, (state, { error }) => ({ ...state, loading: false, error })),
  on(ProductsActions.loadCategoriesSuccess, (state, { categories }) => ({ ...state, categories })),
  on(ProductsActions.loadCategoriesFailure, (state, { error }) => ({ ...state, error }))
);

