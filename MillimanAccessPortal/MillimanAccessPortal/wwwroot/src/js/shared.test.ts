import * as $ from 'jquery';
import * as shared from './shared';

test('filters tree', () => {
  // Arrange
  const exampleTree = `
<li>
  <div id="div-1-1" data-filter-string="FS1"></div>
</li>
<li>
  <div id="div-1-2" data-filter-string="FS2"></div>
</li>
<li>
  <div id="div-1-3" data-filter-string="FS3"></div>
</li>
<li class="hr">
<li>
  <div id="div-2-1" data-filter-string="FS1"></div>
</li>
<li class="hr">
  `
  // Act
  const $base = $(exampleTree);
  const $filtered = shared.filterTree($base, 'fs1');

  // Assert
  expect($filtered.length).toBe(4);
});