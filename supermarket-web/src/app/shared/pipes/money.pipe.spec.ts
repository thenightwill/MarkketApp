import { MoneyPipe } from './money.pipe';

describe('MoneyPipe', () => {
  const pipe = new MoneyPipe();

  it('formats Colombian pesos without decimals for whole amounts', () => {
    expect(pipe.transform(3200).replace(/\s/g, ' ')).toContain('3.200');
  });

  it('treats missing values as zero', () => {
    expect(pipe.transform(null)).toContain('0');
    expect(pipe.transform(undefined)).toContain('0');
  });
});
