'use strict';
define(['app','Cells/NzbDroneCell'], function () {
    NzbDrone.Cells.RelativeDateCell = NzbDrone.Cells.NzbDroneCell.extend({

        className : 'relative-date-cell',

        render: function () {

            var date = this.model.get(this.column.get('name'));
            this.$el.html(Date.create(date).relative());

            return this;
        }
    });
});