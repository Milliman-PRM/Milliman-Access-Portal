import $ = require('jquery');
import shared = require('../shared');

test('filters a tree of li', () => {
  // Arrange
  const $tree = $(`
  <li><div data-filter-string="A"  ></div></li>
  <li><div data-filter-string="AB" ></div></li>
  <li><div data-filter-string="ABC"></div></li>
  <li class="hr"></li>
  <li><div data-filter-string="B"  ></div></li>
  <li><div data-filter-string="BC" ></div></li>
  <li class="hr"></li>
  <li><div data-filter-string="C"  ></div></li>
  <li class="hr"></li>
  `);

  // Act
  const filteredAB = shared.filterTree($tree, 'AB');
  const filteredB  = shared.filterTree($tree, 'B' );
  const filtered   = shared.filterTree($tree, ''  );

  // Assert
  expect(filteredAB.find('div').addBack('div').length).toBe(2);
  expect(filteredAB.find('.hr').addBack('.hr').length).toBe(1);
  expect(filteredB .find('div').addBack('div').length).toBe(4);
  expect(filteredB .find('.hr').addBack('.hr').length).toBe(2);
  expect(filtered  .find('div').addBack('div').length).toBe(6);
  expect(filtered  .find('.hr').addBack('.hr').length).toBe(3);
});

test('filters selections', () => {
  // Arrange
  const $form = $(`
    <form class="admin-panel-content">
      <div class="fieldset-container">
        <fieldset>
          <div class="selection-option-container" data-selection-value="A"  ></div>
          <div class="selection-option-container" data-selection-value="AB" ></div>
          <div class="selection-option-container" data-selection-value="ABC"></div>
        </fieldset>
        <fieldset>
          <div class="selection-option-container" data-selection-value="B"  ></div>
          <div class="selection-option-container" data-selection-value="BC" ></div>
        </fieldset>
        <fieldset>
          <div class="selection-option-container" data-selection-value="C"  ></div>
        </fieldset>
      </div>
    </form>
  `);


  // Act
  const filteredAB = shared.filterSelections($form, 'AB');
  const filteredB  = shared.filterSelections($form, 'B' );
  const filtered   = shared.filterSelections($form, ''  );

  // Assert
  expect(filteredAB.length).toBe(2);
  expect(filteredB .length).toBe(4);
  expect(filtered  .length).toBe(6);
});
