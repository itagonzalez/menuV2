// Global JavaScript functions for Menu Management System

// Utility functions
const Utils = {
    // Show loading overlay
    showLoading: function () {
        $('#loadingOverlay').removeClass('d-none');
    },

    // Hide loading overlay
    hideLoading: function () {
        $('#loadingOverlay').addClass('d-none');
    },

    // Show success message
    showSuccess: function (message, title = 'Éxito') {
        Swal.fire({
            icon: 'success',
            title: title,
            text: message,
            timer: 3000,
            showConfirmButton: false,
            toast: true,
            position: 'top-end'
        });
    },

    // Show error message
    showError: function (message, title = 'Error') {
        Swal.fire({
            icon: 'error',
            title: title,
            text: message,
            confirmButtonText: 'Entendido',
            confirmButtonColor: '#dc3545'
        });
    },

    // Show confirmation dialog
    confirmAction: function (message, title = '¿Está seguro?', callback) {
        Swal.fire({
            title: title,
            text: message,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Sí, continuar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed && callback) {
                callback();
            }
        });
    },

    // Format text for display
    truncateText: function (text, length = 50) {
        if (text.length <= length) return text;
        return text.substring(0, length) + '...';
    },

    // Validate URL format
    isValidUrl: function (string) {
        try {
            new URL(string);
            return true;
        } catch (_) {
            return false;
        }
    },

    // Debounce function for search
    debounce: function (func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
};

// AJAX helpers
const AjaxHelper = {
    // Generic GET request
    get: function (url, onSuccess, onError) {
        $.ajax({
            url: url,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                if (onSuccess) onSuccess(data);
            },
            error: function (xhr, status, error) {
                console.error('AJAX Error:', error);
                if (onError) {
                    onError(xhr, status, error);
                } else {
                    Utils.showError('Error al cargar los datos: ' + error);
                }
            }
        });
    },

    // Generic POST request
    post: function (url, data, onSuccess, onError) {
        $.ajax({
            url: url,
            type: 'POST',
            data: data,
            success: function (response) {
                if (onSuccess) onSuccess(response);
            },
            error: function (xhr, status, error) {
                console.error('AJAX Error:', error);
                if (onError) {
                    onError(xhr, status, error);
                } else {
                    Utils.showError('Error al procesar la solicitud: ' + error);
                }
            }
        });
    },

    // JSON POST request
    postJson: function (url, data, onSuccess, onError) {
        $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function (response) {
                if (onSuccess) onSuccess(response);
            },
            error: function (xhr, status, error) {
                console.error('AJAX Error:', error);
                if (onError) {
                    onError(xhr, status, error);
                } else {
                    Utils.showError('Error al procesar la solicitud: ' + error);
                }
            }
        });
    }
};

// Menu management functions
const MenuManager = {
    // Load menu items dynamically
    loadMenuItems: function (parentId = null, containerId = '#menuContainer') {
        const url = parentId ?
            `/Menu/GetChildren?parentId=${parentId}` :
            '/Menu/GetRootItems';

        AjaxHelper.get(url, function (data) {
            MenuManager.renderMenuItems(data, containerId, parentId ? 1 : 0);
        });
    },

    // Render menu items in the UI
    renderMenuItems: function (items, containerId, level = 0) {
        let html = '';

        items.forEach(item => {
            html += `
                <div class="menu-item menu-level-${level} fade-in" data-id="${item.id}">
                    <div class="d-flex justify-content-between align-items-center">
                        <div class="flex-grow-1">
                            <strong>${item.name}</strong>
                            ${item.link ? `<br><small class="text-muted">${item.link}</small>` : ''}
                            <br><small class="text-info">Orden: ${item.order}</small>
                        </div>
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="MenuManager.editItem(${item.id})">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-sm btn-outline-success" onclick="MenuManager.addChild(${item.id})">
                                <i class="fas fa-plus"></i>
                            </button>
                            <button class="btn btn-sm btn-outline-danger" onclick="MenuManager.deleteItem(${item.id})">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    </div>
                    <div class="children-container mt-2" id="children-${item.id}"></div>
                </div>
            `;
        });

        $(containerId).html(html);

        // Load children for each item
        items.forEach(item => {
            if (item.hasChildren) {
                MenuManager.loadMenuItems(item.id, `#children-${item.id}`);
            }
        });
    },

    // Edit menu item
    editItem: function (id) {
        window.location.href = `/Menu/Edit/${id}`;
    },

    // Add child item
    addChild: function (parentId) {
        window.location.href = `/Menu/Create?parentId=${parentId}`;
    },

    // Delete menu item
    deleteItem: function (id) {
        Utils.confirmAction(
            'Esta acción eliminará el elemento del menú y todos sus hijos. ¿Desea continuar?',
            'Confirmar eliminación',
            function () {
                AjaxHelper.post(`/Menu/Delete/${id}`, {}, function (response) {
                    if (response.success) {
                        Utils.showSuccess('Elemento eliminado correctamente');
                        location.reload();
                    } else {
                        Utils.showError(response.message || 'Error al eliminar el elemento');
                    }
                });
            }
        );
    }
};

// Role management functions
const RoleManager = {
    // Delete role
    deleteRole: function (id, name) {
        Utils.confirmAction(
            `¿Está seguro de eliminar el rol "${name}"? Esta acción no se puede deshacer.`,
            'Confirmar eliminación',
            function () {
                AjaxHelper.post(`/Roles/Delete/${id}`, {}, function (response) {
                    if (response.success) {
                        Utils.showSuccess('Rol eliminado correctamente');
                        location.reload();
                    } else {
                        Utils.showError(response.message || 'Error al eliminar el rol');
                    }
                });
            }
        );
    }
};

// Permission management functions
const PermissionManager = {
    // Toggle permission
    togglePermission: function (roleId, menuItemId, checkbox) {
        const isActive = checkbox.checked;

        AjaxHelper.postJson('/Permissions/Toggle', {
            roleId: roleId,
            menuItemId: menuItemId,
            isActive: isActive
        }, function (response) {
            if (response.success) {
                Utils.showSuccess(`Permiso ${isActive ? 'activado' : 'desactivado'} correctamente`);
                $(checkbox).closest('tr').toggleClass('table-success', isActive);
            } else {
                // Revert checkbox state
                checkbox.checked = !isActive;
                Utils.showError(response.message || 'Error al actualizar el permiso');
            }
        }, function () {
            // Revert checkbox state on error
            checkbox.checked = !isActive;
        });
    },

    // Load permissions matrix
    loadPermissionsMatrix: function () {
        AjaxHelper.get('/Permissions/GetMatrix', function (data) {
            PermissionManager.renderPermissionsMatrix(data);
        });
    },

    // Render permissions matrix
    renderPermissionsMatrix: function (data) {
        // Implementation would depend on your specific matrix structure
        console.log('Permissions matrix data:', data);
    }
};

// Demo page functions
const DemoManager = {
    // Load menu for selected role
    loadRoleMenu: function (roleId) {
        if (!roleId) {
            $('#dynamicMenuContainer').html('<div class="text-muted text-center p-4">Seleccione un rol para ver el menú</div>');
            return;
        }

        Utils.showLoading();

        AjaxHelper.get(`/Menu/GetMenuForRole/${roleId}`, function (data) {
            DemoManager.renderRoleMenu(data);
            Utils.hideLoading();
        }, function () {
            $('#dynamicMenuContainer').html('<div class="text-danger text-center p-4">Error al cargar el menú</div>');
            Utils.hideLoading();
        });
    },

    // Render menu for role
    renderRoleMenu: function (menuItems) {
        if (!menuItems || menuItems.length === 0) {
            $('#dynamicMenuContainer').html('<div class="text-muted text-center p-4">No hay elementos de menú disponibles para este rol</div>');
            return;
        }

        let html = '<ul class="nav nav-pills flex-column">';

        function renderMenuLevel(items, level = 0) {
            items.forEach(item => {
                const paddingLeft = level * 20;
                html += `
                    <li class="nav-item" style="padding-left: ${paddingLeft}px;">
                        <a class="nav-link ${item.link ? 'text-primary' : 'text-dark'}" 
                           href="${item.link || '#'}" 
                           ${item.link ? (item.openMode === 'new_tab' ? 'target="_blank"' : '') : ''}
                           onclick="DemoManager.handleMenuClick('${item.name}', '${item.link || ''}')">
                            <i class="fas fa-${level === 0 ? 'folder' : 'file'} me-2"></i>
                            ${item.name}
                            ${item.link ? '<i class="fas fa-external-link-alt ms-2 small"></i>' : ''}
                        </a>
                    </li>
                `;

                if (item.children && item.children.length > 0) {
                    renderMenuLevel(item.children, level + 1);
                }
            });
        }

        renderMenuLevel(menuItems);
        html += '</ul>';

        $('#dynamicMenuContainer').html(html).addClass('fade-in');
    },

    // Handle menu click
    handleMenuClick: function (itemName, link) {
        if (link) {
            console.log(`Navegando a: ${itemName} - ${link}`);
        } else {
            Utils.showSuccess(`Clicked en: ${itemName}`);
        }
    }
};

// Form validation helpers
const FormValidator = {
    // Validate menu item form
    validateMenuForm: function (formId) {
        const form = $(formId);
        let isValid = true;

        // Clear previous errors
        form.find('.is-invalid').removeClass('is-invalid');
        form.find('.invalid-feedback').remove();

        // Validate name
        const name = form.find('#Name').val().trim();
        if (!name) {
            FormValidator.showFieldError('#Name', 'El nombre es obligatorio');
            isValid = false;
        } else if (name.length > 30) {
            FormValidator.showFieldError('#Name', 'El nombre no puede exceder 30 caracteres');
            isValid = false;
        }

        // Validate link if provided
        const link = form.find('#Link').val().trim();
        if (link) {
            if (link.length > 200) {
                FormValidator.showFieldError('#Link', 'El link no puede exceder 200 caracteres');
                isValid = false;
            } else if (!Utils.isValidUrl(link)) {
                FormValidator.showFieldError('#Link', 'El formato del link no es válido');
                isValid = false;
            }
        }

        // Validate order
        const order = form.find('#Order').val();
        if (!order || isNaN(order) || order < 0) {
            FormValidator.showFieldError('#Order', 'El orden debe ser un número válido');
            isValid = false;
        }

        return isValid;
    },

    // Show field error
    showFieldError: function (fieldId, message) {
        const field = $(fieldId);
        field.addClass('is-invalid');
        field.after(`<div class="invalid-feedback">${message}</div>`);
    },

    // Validate role form
    validateRoleForm: function (formId) {
        const form = $(formId);
        let isValid = true;

        // Clear previous errors
        form.find('.is-invalid').removeClass('is-invalid');
        form.find('.invalid-feedback').remove();

        // Validate name
        const name = form.find('#Name').val().trim();
        if (!name) {
            FormValidator.showFieldError('#Name', 'El nombre del rol es obligatorio');
            isValid = false;
        } else if (name.length > 50) {
            FormValidator.showFieldError('#Name', 'El nombre no puede exceder 50 caracteres');
            isValid = false;
        }

        return isValid;
    }
};

// Search functionality
const SearchManager = {
    // Initialize search
    initSearch: function (inputId, targetContainer, searchUrl) {
        const debouncedSearch = Utils.debounce(function (query) {
            SearchManager.performSearch(query, targetContainer, searchUrl);
        }, 300);

        $(inputId).on('input', function () {
            const query = $(this).val().trim();
            if (query.length > 2 || query.length === 0) {
                debouncedSearch(query);
            }
        });
    },

    // Perform search
    performSearch: function (query, targetContainer, searchUrl) {
        AjaxHelper.get(`${searchUrl}?q=${encodeURIComponent(query)}`, function (data) {
            // Handle search results based on context
            $(targetContainer).html(data);
        });
    }
};

// Initialize when document is ready
$(document).ready(function () {
    // Add fade-in animation to main content
    $('main').addClass('fade-in');

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Auto-focus first input in forms
    $('form input[type="text"], form input[type="email"], form textarea').first().focus();

    // Add loading state to form submissions
    $('form').on('submit', function () {
        const submitBtn = $(this).find('button[type="submit"]');
        submitBtn.prop('disabled', true);
        submitBtn.html('<span class="spinner-border spinner-border-sm me-2"></span>Procesando...');

        // Re-enable after 10 seconds as fallback
        setTimeout(function () {
            submitBtn.prop('disabled', false);
            submitBtn.html(submitBtn.data('original-text') || 'Guardar');
        }, 10000);
    });

    // Store original button text
    $('button[type="submit"]').each(function () {
        $(this).data('original-text', $(this).html());
    });

    // Handle external links
    $('a[href^="http"]').attr('target', '_blank').addClass('external-link');

    // Add confirmation to dangerous actions
    $('.btn-danger[data-confirm]').on('click', function (e) {
        e.preventDefault();
        const message = $(this).data('confirm') || '¿Está seguro de realizar esta acción?';
        const href = $(this).attr('href') || $(this).data('href');

        Utils.confirmAction(message, '¿Está seguro?', function () {
            if (href) {
                window.location.href = href;
            }
        });
    });

    // Initialize any page-specific functionality
    const page = $('body').data('page');
    switch (page) {
        case 'menu-index':
            MenuManager.loadMenuItems();
            break;
        case 'demo':
            // Demo page initialization handled by page-specific script
            break;
        case 'permissions':
            PermissionManager.loadPermissionsMatrix();
            break;
    }

    console.log('Menu Management System initialized successfully');
});