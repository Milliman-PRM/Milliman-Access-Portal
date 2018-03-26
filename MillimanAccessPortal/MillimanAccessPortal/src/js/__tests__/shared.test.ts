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
