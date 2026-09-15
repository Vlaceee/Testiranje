import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ProductsState, productsAdapter } from './products.reducer';

export const selectProductsState = createFeatureSelector<ProductsState>('products');
const entitySelectors = productsAdapter.getSelectors(selectProductsState);
export const selectAllProducts = entitySelectors.selectAll;
export const selectProductsLoading = createSelector(selectProductsState, state => state.loading);
export const selectProductsError = createSelector(selectProductsState, state => state.error);
export const selectProductCategories = createSelector(selectProductsState, state => state.categories);
export const selectProductPagination = createSelector(selectProductsState, state => ({
  page: state.page, pageSize: state.pageSize, totalCount: state.totalCount, totalPages: state.totalPages
}));

