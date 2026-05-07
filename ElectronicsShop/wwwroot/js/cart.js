(function () {
    if (window.__cartBindingsAttached) {
        console.log('cart.js: bindings already attached');
        return;
    }
    window.__cartBindingsAttached = true;

    console.log('cart.js loaded');

    if (typeof jQuery === 'undefined') {
        console.error('cart.js: jQuery not found');
        return;
    }
    var $ = jQuery;

    var antiToken = $('input[name="__RequestVerificationToken"]').val() || null;

    var addUrl = window.cartAddUrl || '/Cart/Add';
    var removeUrl = window.cartRemoveUrl || '/Cart/Remove';
    var loginUrl = window.cartLoginUrl || '/Account/Login';

    function postAjax(url, data, cb) {
        if (antiToken) data.__RequestVerificationToken = antiToken;
        $.ajax({
            url: url,
            method: 'POST',
            data: data,
            success: cb,
            error: function (xhr, status, err) {
                console.log('cart AJAX error', status, err);
                window.location.reload();
            }
        });
    }

    function formatCurrencyVND(amount) {
        try {
            return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
        } catch (e) {
            return amount;
        }
    }

    function bindAddButtons() {
        $('.add-to-cart-btn').off('click.cart').on('click.cart', function (e) {
            var $btn = $(this);
            var $form = $btn.closest('form');
            if (!$form.length) return;

            if ($form.data('no-ajax') || $btn.data('no-ajax')) {
                return;
            }

            e.preventDefault();

            var productId = $form.find('input[name="productId"]').val();
            var quantity = $form.find('input[name="quantity"]').val() || 1;
            if (!productId) return;

            console.log('cart add clicked, productId=', productId, 'quantity=', quantity);

            postAjax(addUrl, { productId: productId, quantity: quantity }, function (res) {
                if (!res) { window.location.reload(); return; }
                if (res.redirectUrl) { window.location = res.redirectUrl; return; }
                if (res.success) {
                    if (typeof showToast === 'function') {
                        showToast(res.message || "Đã thêm vào giỏ hàng", "success");
                    }
                    // update header counts
                    if (typeof res.quantityCount !== 'undefined') {
                        $('.cart-count').text(res.quantityCount);
                    }
                    if (typeof res.rowCount !== 'undefined') {
                        $('#cart-row-count').text(res.rowCount);
                    }
                    if (typeof res.cartSubtotal !== 'undefined') {
                        $('#cart-total-amount').text(formatCurrencyVND(res.cartSubtotal));
                        $('.cart-summary h5:contains("Tổng tiền")').text('Tổng tiền: ' + formatCurrencyVND(res.cartSubtotal));
                    }
                    if (res.html) {
                        $('#cart-dropdown .cart-dropdown').html(res.html);
                        bindRemoveButtons();
                    }
                } else {
                    if (typeof showToast === 'function') {
                        showToast(res.message || "Có lỗi xảy ra", "error");
                    } else {
                        alert(res.message);
                    }
                }
            });
        });
    }

    function bindRemoveButtons() {
        $('.cart-remove').off('click.cart').on('click.cart', function (e) {
            e.preventDefault();
            var $btn = $(this);
            var cartId = $btn.data('cartid');
            var productId = $btn.data('productid');

            if (!cartId) {
                console.log('cart.js: missing cartId');
                return;
            }

            postAjax(removeUrl, { cartId: cartId }, function (res) {
                if (!res) { window.location.reload(); return; }
                if (res.redirectUrl) { window.location = res.redirectUrl; return; }
                if (res.success) {
                    showToast(res.message, "success");
                    // update header quantity count if provided
                    if (typeof res.quantityCount !== 'undefined') {
                        $('.cart-count').text(res.quantityCount);
                    }
                    // update row count on page (number of distinct items)
                    if (typeof res.rowCount !== 'undefined') {
                        $('#cart-row-count').text(res.rowCount);
                    } else {
                        $('#cart-row-count').text($('#shoppingCart tbody tr').length - 1);
                    }

                    // update header subtotal and page total
                    if (typeof res.cartSubtotal !== 'undefined') {
                        $('.cart-summary h5:contains("Tổng tiền")').text('Tổng tiền: ' + formatCurrencyVND(res.cartSubtotal));
                        $('#cart-total-amount').text(formatCurrencyVND(res.cartSubtotal));
                    }

                    // remove the row in the cart page if present
                    var $row = $btn.closest('tr[data-cartid="' + cartId + '"]');
                    if ($row.length) {
                        $row.remove();
                    } else {
                        // fallback: remove closest tr
                        $btn.closest('tr').remove();
                    }

                    // update dropdown HTML if provided
                    if (res.html) {
                        $('#cart-dropdown .cart-dropdown').html(res.html);
                        bindRemoveButtons();
                    }

                    // if no more rows, replace table with empty message
                    if ($('#shoppingCart tbody tr').length === 0) {
                        $('#shoppingCart').replaceWith('<p id="cart-empty-message">Giỏ hàng trống.</p>');
                        $('#cart-total-wrapper').remove();
                    }
                } else {
                    window.location.reload();
                }
            });
        });
    }

    $(function () {
        bindAddButtons();
        bindRemoveButtons();
    });

})();
