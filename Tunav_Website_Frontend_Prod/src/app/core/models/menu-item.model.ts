export interface MenuItem {
  label: string;
  path: string;
  icon?: string;
}

/** Élément de menu pouvant avoir des enfants (dropdown) */
export interface MenuItemWithChildren extends Omit<MenuItem, 'path'> {
  path?: string;
  children?: MenuItem[];
}

